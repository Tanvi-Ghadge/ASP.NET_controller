using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using MyApi.AI.Harness;
using MyApi.AI.Models;
using MyApi.AI.Streaming;

namespace MyApi.Controllers;

/// <summary>
/// Chat HTTP endpoint. Delegates all AI execution to <see cref="IAgentHarness"/>.
/// Controllers do not publish AG-UI events — the harness owns the timeline.
/// </summary>
[ApiController]
[Route("api/chat")]
public sealed class ChatController : ControllerBase
{
    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IAgentHarness _harness;
    private readonly IStreamingService _streaming;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IAgentHarness harness,
        IStreamingService streaming,
        ILogger<ChatController> logger)
    {
        _harness = harness;
        _streaming = streaming;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/chat — execute a harness-managed conversational turn (synchronous final response).
    /// Real-time clients should also subscribe via SignalR <c>/hubs/agent</c>.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request?.Message))
        {
            return BadRequest(new { error = "Message is required." });
        }

        try
        {
            _logger.LogInformation(
                "Chat endpoint received request. SessionId={SessionId}, UserId={UserId}",
                string.IsNullOrWhiteSpace(request.SessionId) ? "(new)" : request.SessionId,
                string.IsNullOrWhiteSpace(request.UserId) ? "(default)" : request.UserId);

            var result = await _harness.ExecuteAsync(
                request.SessionId,
                request.UserId,
                request.Message,
                cancellationToken);

            if (!result.Succeeded)
            {
                _logger.LogWarning(
                    "Harness reported failure. CorrelationId={CorrelationId}, Error={Error}",
                    result.CorrelationId,
                    result.Error);
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }

            var response = new ChatResponse(result.SessionId, result.Response)
            {
                ApprovalRequired = result.ApprovalPending,
                WorkflowInstanceId = result.WorkflowInstanceId,
                ApprovalRequestId = result.ApprovalRequestId,
                Status = result.ApprovalPending ? "ApprovalRequired" : "Completed"
            };

            if (result.ApprovalPending)
            {
                return Accepted(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat endpoint failed before harness completion.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        }
    }

    /// <summary>
    /// GET /api/chat/stream — Server-Sent Events demo for Swagger / non-SignalR clients.
    /// React clients should prefer SignalR at <c>/hubs/agent</c>.
    /// </summary>
    [HttpGet("stream")]
    [Produces("text/event-stream")]
    public Task StreamGet(
        [FromQuery(Name = "message")] string message,
        [FromQuery(Name = "session_id")] string? sessionId,
        [FromQuery(Name = "user_id")] string? userId,
        CancellationToken cancellationToken) =>
        WriteSseAsync(sessionId, userId, message, cancellationToken);

    /// <summary>
    /// POST /api/chat/stream — SSE with JSON body (easier than long query strings).
    /// </summary>
    [HttpPost("stream")]
    [Produces("text/event-stream")]
    public Task StreamPost(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request?.Message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return Response.WriteAsync("{\"error\":\"Message is required.\"}", cancellationToken);
        }

        return WriteSseAsync(request.SessionId, request.UserId, request.Message, cancellationToken);
    }

    private async Task WriteSseAsync(
        string? sessionId,
        string? userId,
        string message,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            await Response.WriteAsync("{\"error\":\"Message is required.\"}", cancellationToken);
            return;
        }

        var correlationId = Guid.NewGuid().ToString("N");
        var streamingContext = new StreamingContext
        {
            CorrelationId = correlationId,
            SessionId = sessionId,
            UserId = userId
        };

        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.ContentType = "text/event-stream";

        // Controller owns the SSE channel lifetime; harness RegisterExecution is idempotent.
        using var registration = _streaming.RegisterExecution(correlationId);

        var executeTask = _harness.ExecuteAsync(
            sessionId,
            userId,
            message,
            streamingContext,
            cancellationToken);

        try
        {
            await foreach (var evt in _streaming.SubscribeAsync(correlationId, cancellationToken))
            {
                var json = JsonSerializer.Serialize(evt, SseJsonOptions);
                await Response.WriteAsync($"event: {evt.Type}\n", cancellationToken);
                await Response.WriteAsync($"id: {evt.Sequence}\n", cancellationToken);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                if (evt.Type == StreamingEventType.ExecutionFinished)
                {
                    break;
                }
            }
        }
        finally
        {
            var result = await executeTask;
            registration.Dispose();
            _logger.LogInformation(
                "SSE stream finished. CorrelationId={CorrelationId}, Succeeded={Succeeded}, SessionId={SessionId}",
                result.CorrelationId,
                result.Succeeded,
                result.SessionId);
        }
    }
}

using MyApi.AI.AGUI;
using MyApi.AI.Streaming;

namespace MyApi.AI.Middleware;

/// <summary>
/// Outermost middleware: catches unhandled exceptions and publishes AG-UI errors.
/// Lives in <c>MyApi.AI.Middleware</c> (distinct from ASP.NET exception middleware).
/// </summary>
public sealed class ExceptionMiddleware : IAgentMiddleware
{
    private readonly IStreamingService _streaming;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        IStreamingService streaming,
        ILogger<ExceptionMiddleware> logger)
    {
        _streaming = streaming;
        _logger = logger;
    }

    public string Name => "Exception";
    public int Order => 0;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            context.RejectedReason = ex.Message;
            context.Telemetry.Mark($"Exception:{ex.GetType().Name}");

            _logger.LogError(
                ex,
                "Unhandled AI pipeline exception. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, SessionId={SessionId}, UserId={UserId}",
                context.ExecutionId,
                context.CorrelationId,
                context.SessionId,
                context.UserId);

            if (context.Streaming is not null)
            {
                await _streaming.PublishAsync(
                    context.Streaming,
                    new ErrorEvent
                    {
                        CorrelationId = context.CorrelationId,
                        Message = ex.Message,
                        Source = "AgentMiddlewarePipeline"
                    },
                    context.CancellationToken);
            }

            throw;
        }
    }
}

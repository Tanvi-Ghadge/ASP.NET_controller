using MyApi.AI.AGUI;
using MyApi.AI.Models;
using MyApi.AI.Observability;
using MyApi.AI.Streaming;
using Microsoft.Extensions.Options;

namespace MyApi.AI.Middleware;

/// <summary>
/// Collects execution metrics and publishes AG-UI telemetry timeline events.
/// </summary>
public sealed class TelemetryMiddleware : IAgentMiddleware
{
    private readonly IStreamingService _streaming;
    private readonly IOptions<AgentFrameworkOptions> _options;
    private readonly ILogger<TelemetryMiddleware> _logger;

    public TelemetryMiddleware(
        IStreamingService streaming,
        IOptions<AgentFrameworkOptions> options,
        ILogger<TelemetryMiddleware> logger)
    {
        _streaming = streaming;
        _options = options;
        _logger = logger;
    }

    public string Name => "Telemetry";
    public int Order => 70;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        context.Telemetry.Model = _options.Value.Model;
        context.Telemetry.Temperature = 1.0;
        context.Telemetry.TopP = 1.0;
        context.Telemetry.Mark("Telemetry.Start");

        using var activity = AiActivitySource.StartActivity("ai.telemetry");
        activity?.SetTag("execution.id", context.ExecutionId);
        activity?.SetTag("correlation.id", context.CorrelationId);

        try
        {
            await next(context);
            context.Telemetry.FinishReason = context.OrchestrationResult?.ApprovalPending == true
                ? "approval_pending"
                : (context.OrchestrationResult?.Succeeded == true ? "stop" : "error");
        }
        finally
        {
            sw.Stop();
            context.Telemetry.TotalDurationMs = sw.ElapsedMilliseconds;
            context.Telemetry.AgentDurationMs = sw.ElapsedMilliseconds;
            context.Telemetry.ResponseLength = (context.FilteredResponse ?? context.FinalResponse)?.Length ?? 0;

            if (context.OrchestrationResult is not null)
            {
                context.Telemetry.WorkflowId = context.OrchestrationResult.Plan.SelectedWorkflowId;
                context.Telemetry.WorkflowName = context.OrchestrationResult.Plan.SelectedWorkflowName;
                context.Telemetry.AgentKey = context.OrchestrationResult.Plan.SelectedAgentKey;
                context.Telemetry.AgentName = context.OrchestrationResult.Plan.Metadata.TryGetValue("AgentName", out var n)
                    ? n?.ToString()
                    : context.Telemetry.AgentKey;
            }

            context.Telemetry.Mark("Telemetry.Complete");

            _logger.LogInformation(
                "Execution telemetry. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, SessionId={SessionId}, UserId={UserId}, WorkflowId={WorkflowId}, Agent={Agent}, DurationMs={DurationMs}, MemoryRetrieved={MemoryRetrieved}, Tools={ToolCount}, PromptTokens={PromptTokens}, CompletionTokens={CompletionTokens}, EstimatedCostUsd={EstimatedCostUsd}",
                context.ExecutionId,
                context.CorrelationId,
                context.SessionId,
                context.UserId,
                context.Telemetry.WorkflowId,
                context.Telemetry.AgentName,
                context.Telemetry.TotalDurationMs,
                context.Telemetry.MemoryRetrieved,
                context.Telemetry.Tools.Count,
                context.Telemetry.PromptTokens,
                context.Telemetry.CompletionTokens,
                context.Telemetry.EstimatedCostUsd);

            if (context.Streaming is not null)
            {
                await _streaming.PublishAsync(
                    context.Streaming,
                    new TelemetrySnapshotEvent
                    {
                        CorrelationId = context.CorrelationId,
                        ExecutionId = context.ExecutionId,
                        WorkflowId = context.Telemetry.WorkflowId,
                        AgentName = context.Telemetry.AgentName,
                        DurationMs = context.Telemetry.TotalDurationMs,
                        ToolCount = context.Telemetry.Tools.Count,
                        EstimatedCostUsd = context.Telemetry.EstimatedCostUsd,
                        Timeline = context.Telemetry.Timeline.ToArray()
                    },
                    context.CancellationToken);
            }
        }
    }

    public async Task OnBeforeToolAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        context.Telemetry.Mark($"Tool.Started:{context.CurrentToolName}");
        if (context.Streaming is not null)
        {
            await _streaming.PublishAsync(
                context.Streaming,
                new ToolStartedEvent
                {
                    CorrelationId = context.CorrelationId,
                    ToolName = context.CurrentToolName ?? "unknown",
                    AgentName = context.CurrentPluginName
                },
                cancellationToken);
        }
    }

    public async Task OnAfterToolAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        context.Telemetry.Mark(
            context.CurrentToolSucceeded
                ? $"Tool.Completed:{context.CurrentToolName}"
                : $"Tool.Failed:{context.CurrentToolName}");

        if (context.Streaming is not null)
        {
            await _streaming.PublishAsync(
                context.Streaming,
                new ToolCompletedEvent
                {
                    CorrelationId = context.CorrelationId,
                    ToolName = context.CurrentToolName ?? "unknown",
                    AgentName = context.CurrentPluginName,
                    Succeeded = context.CurrentToolSucceeded,
                    DurationMs = context.Telemetry.Tools.LastOrDefault()?.DurationMs ?? 0
                },
                cancellationToken);
        }
    }
}

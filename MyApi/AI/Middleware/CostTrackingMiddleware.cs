namespace MyApi.AI.Middleware;

/// <summary>
/// Estimates token usage and USD cost from prompt/response lengths.
/// </summary>
public sealed class CostTrackingMiddleware : IAgentMiddleware
{
    private const decimal InputPer1K = 0.00015m;
    private const decimal OutputPer1K = 0.0006m;

    private readonly ILogger<CostTrackingMiddleware> _logger;

    public CostTrackingMiddleware(ILogger<CostTrackingMiddleware> logger)
    {
        _logger = logger;
    }

    public string Name => "CostTracking";
    public int Order => 80;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        await next(context);

        var prompt = context.MaskedUserMessage ?? context.UserMessage;
        var response = context.FilteredResponse ?? context.FinalResponse ?? string.Empty;
        var promptTokens = EstimateTokens(prompt);
        var completionTokens = EstimateTokens(response);

        context.Telemetry.PromptTokens = promptTokens;
        context.Telemetry.CompletionTokens = completionTokens;
        context.Telemetry.EstimatedCostUsd =
            (promptTokens / 1000m) * InputPer1K + (completionTokens / 1000m) * OutputPer1K;

        context.Telemetry.Mark(
            $"Cost.EstimatedTokens={context.Telemetry.TotalTokens};Usd={context.Telemetry.EstimatedCostUsd:F6}");

        _logger.LogInformation(
            "Cost tracked. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, Model={Model}, PromptTokens={PromptTokens}, CompletionTokens={CompletionTokens}, TotalTokens={TotalTokens}, EstimatedCostUsd={EstimatedCostUsd}",
            context.ExecutionId,
            context.CorrelationId,
            context.Telemetry.Model,
            promptTokens,
            completionTokens,
            context.Telemetry.TotalTokens,
            context.Telemetry.EstimatedCostUsd);
    }

    private static int EstimateTokens(string text) =>
        string.IsNullOrEmpty(text) ? 0 : Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
}

namespace MyApi.AI.Middleware;

/// <summary>
/// Logs prompts with sensitive fields masked.
/// </summary>
public sealed class PromptLoggingMiddleware : IAgentMiddleware
{
    private readonly ILogger<PromptLoggingMiddleware> _logger;

    public PromptLoggingMiddleware(ILogger<PromptLoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public string Name => "PromptLogging";
    public int Order => 40;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        context.MaskedUserMessage = SensitiveDataMasker.Mask(context.UserMessage);
        context.Telemetry.Mark("PromptLogging.Logged");

        _logger.LogInformation(
            "Prompt logged. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, SessionId={SessionId}, UserId={UserId}, MaskedPrompt={MaskedPrompt}",
            context.ExecutionId,
            context.CorrelationId,
            context.SessionId,
            context.UserId,
            context.MaskedUserMessage);

        await next(context);
    }
}

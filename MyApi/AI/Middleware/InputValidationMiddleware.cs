namespace MyApi.AI.Middleware;

/// <summary>
/// Validates user message length and basic content rules.
/// </summary>
public sealed class InputValidationMiddleware : IAgentMiddleware
{
    private readonly ILogger<InputValidationMiddleware> _logger;

    public InputValidationMiddleware(ILogger<InputValidationMiddleware> logger)
    {
        _logger = logger;
    }

    public string Name => "InputValidation";
    public int Order => 30;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        if (string.IsNullOrWhiteSpace(context.UserMessage))
        {
            throw new ArgumentException("User message is required.");
        }

        if (context.UserMessage.Length > 8000)
        {
            throw new ArgumentException("User message exceeds maximum length of 8000 characters.");
        }

        context.Telemetry.PromptLength = context.UserMessage.Length;
        context.Telemetry.Mark("InputValidation.Passed");
        _logger.LogInformation(
            "Input validated. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, PromptLength={PromptLength}",
            context.ExecutionId,
            context.CorrelationId,
            context.UserMessage.Length);

        await next(context);
    }
}

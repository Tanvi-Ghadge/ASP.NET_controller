namespace MyApi.AI.Middleware;

/// <summary>
/// Masks confidential fields in the final response before returning to clients.
/// </summary>
public sealed class ResponseFilterMiddleware : IAgentMiddleware
{
    private readonly ILogger<ResponseFilterMiddleware> _logger;

    public ResponseFilterMiddleware(ILogger<ResponseFilterMiddleware> logger)
    {
        _logger = logger;
    }

    public string Name => "ResponseFilter";
    public int Order => 90;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        await next(context);

        var raw = context.FinalResponse ?? context.OrchestrationResult?.Output ?? string.Empty;
        context.FilteredResponse = SensitiveDataMasker.Mask(raw);
        context.Telemetry.Mark("ResponseFilter.Applied");

        _logger.LogInformation(
            "Response filtered. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, ResponseLength={ResponseLength}",
            context.ExecutionId,
            context.CorrelationId,
            context.FilteredResponse.Length);
    }
}

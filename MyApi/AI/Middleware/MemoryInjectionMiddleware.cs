namespace MyApi.AI.Middleware;

/// <summary>
/// Ensures memory already loaded by the harness is reflected on telemetry / context.
/// </summary>
public sealed class MemoryInjectionMiddleware : IAgentMiddleware
{
    private readonly ILogger<MemoryInjectionMiddleware> _logger;

    public MemoryInjectionMiddleware(ILogger<MemoryInjectionMiddleware> logger)
    {
        _logger = logger;
    }

    public string Name => "MemoryInjection";
    public int Order => 50;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        var count = context.Harness?.Memory.Memories.Count ?? 0;
        context.Telemetry.MemoryRetrieved = count;
        context.Telemetry.Mark($"MemoryInjection.Count={count}");

        _logger.LogInformation(
            "Memory injected into execution context. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, UserId={UserId}, MemoryCount={MemoryCount}",
            context.ExecutionId,
            context.CorrelationId,
            context.UserId,
            count);

        await next(context);
    }
}

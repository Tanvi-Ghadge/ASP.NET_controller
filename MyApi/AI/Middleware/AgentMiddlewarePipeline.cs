using System.Diagnostics;
using MyApi.AI.Observability;

namespace MyApi.AI.Middleware;

/// <summary>
/// Builds and executes the ordered AI middleware onion around a terminal (orchestrator).
/// Harness never invokes individual middleware — only this pipeline.
/// </summary>
public interface IAgentMiddlewarePipeline
{
    /// <summary>
    /// Runs BeforeAgent → terminal → AfterAgent → BeforeResponse middleware chain.
    /// </summary>
    Task ExecuteAsync(
        AgentExecutionContext context,
        Func<AgentExecutionContext, Task> terminal,
        CancellationToken cancellationToken = default);

    /// <summary>Runs BeforeTool hooks for all registered middleware.</summary>
    Task InvokeBeforeToolAsync(AgentExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Runs AfterTool hooks for all registered middleware.</summary>
    Task InvokeAfterToolAsync(AgentExecutionContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default ordered middleware pipeline.
/// </summary>
public sealed class AgentMiddlewarePipeline : IAgentMiddlewarePipeline
{
    private readonly IReadOnlyList<IAgentMiddleware> _middleware;
    private readonly ILogger<AgentMiddlewarePipeline> _logger;

    public AgentMiddlewarePipeline(
        IEnumerable<IAgentMiddleware> middleware,
        ILogger<AgentMiddlewarePipeline> logger)
    {
        _middleware = middleware.OrderBy(m => m.Order).ThenBy(m => m.Name).ToList();
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(
        AgentExecutionContext context,
        Func<AgentExecutionContext, Task> terminal,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(terminal);

        context.CancellationToken = cancellationToken;

        using var activity = AiActivitySource.StartActivity("ai.middleware.pipeline");
        activity?.SetTag("execution.id", context.ExecutionId);
        activity?.SetTag("correlation.id", context.CorrelationId);
        activity?.SetTag("user.id", context.UserId);
        activity?.SetTag("session.id", context.SessionId);

        _logger.LogInformation(
            "AI middleware pipeline starting. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, SessionId={SessionId}, UserId={UserId}, MiddlewareCount={Count}",
            context.ExecutionId,
            context.CorrelationId,
            context.SessionId,
            context.UserId,
            _middleware.Count);

        AgentMiddlewareDelegate pipeline = async ctx =>
        {
            using var terminalActivity = AiActivitySource.StartActivity("ai.orchestrator");
            await terminal(ctx);
        };

        // Build onion: last registered (highest order) is closest to terminal when we reverse-fold.
        for (var i = _middleware.Count - 1; i >= 0; i--)
        {
            var current = _middleware[i];
            var next = pipeline;
            pipeline = async ctx =>
            {
                using var mwActivity = AiActivitySource.StartActivity($"ai.middleware.{current.Name}");
                mwActivity?.SetTag("middleware.name", current.Name);
                mwActivity?.SetTag("middleware.order", current.Order);
                mwActivity?.SetTag("execution.id", ctx.ExecutionId);
                mwActivity?.SetTag("correlation.id", ctx.CorrelationId);

                _logger.LogDebug(
                    "Middleware enter. Middleware={Middleware}, Order={Order}, ExecutionId={ExecutionId}, CorrelationId={CorrelationId}",
                    current.Name,
                    current.Order,
                    ctx.ExecutionId,
                    ctx.CorrelationId);

                await current.InvokeAsync(ctx, next);

                _logger.LogDebug(
                    "Middleware exit. Middleware={Middleware}, ExecutionId={ExecutionId}, CorrelationId={CorrelationId}",
                    current.Name,
                    ctx.ExecutionId,
                    ctx.CorrelationId);
            };
        }

        await pipeline(context);

        _logger.LogInformation(
            "AI middleware pipeline finished. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, Rejected={Rejected}, DurationMs={DurationMs}",
            context.ExecutionId,
            context.CorrelationId,
            context.RejectedReason is not null,
            context.Telemetry.TotalDurationMs);
    }

    /// <inheritdoc />
    public async Task InvokeBeforeToolAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var mw in _middleware)
        {
            await mw.OnBeforeToolAsync(context, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task InvokeAfterToolAsync(
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        foreach (var mw in _middleware)
        {
            await mw.OnAfterToolAsync(context, cancellationToken);
        }
    }
}

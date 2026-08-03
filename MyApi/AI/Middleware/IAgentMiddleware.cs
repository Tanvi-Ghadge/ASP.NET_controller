namespace MyApi.AI.Middleware;

/// <summary>
/// Cross-cutting AI middleware. Implementations must not own business domain logic.
/// </summary>
public interface IAgentMiddleware
{
    /// <summary>Stable middleware name for logs and telemetry.</summary>
    string Name { get; }

    /// <summary>Lower runs earlier. Ordering is deterministic.</summary>
    int Order { get; }

    /// <summary>
    /// Invokes this middleware for an agent-level stage (BeforeAgent / AfterAgent / BeforeResponse).
    /// Must call <paramref name="next"/> to continue unless rejecting / short-circuiting.
    /// </summary>
    Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next);

    /// <summary>Optional hook before a tool/plugin method runs.</summary>
    Task OnBeforeToolAsync(AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    /// <summary>Optional hook after a tool/plugin method runs.</summary>
    Task OnAfterToolAsync(AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

/// <summary>
/// Next middleware (or terminal) in the onion chain.
/// </summary>
public delegate Task AgentMiddlewareDelegate(AgentExecutionContext context);

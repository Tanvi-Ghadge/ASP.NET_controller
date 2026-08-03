namespace MyApi.AI.Middleware;

/// <summary>
/// Outcome of a middleware hop. Middleware may continue, reject, or short-circuit with a response.
/// </summary>
public sealed class MiddlewareResult
{
    public MiddlewareDisposition Disposition { get; init; }

    public string? Reason { get; init; }

    public string? ShortCircuitResponse { get; init; }

    public static MiddlewareResult Continue() =>
        new() { Disposition = MiddlewareDisposition.Continue };

    public static MiddlewareResult Reject(string reason) =>
        new() { Disposition = MiddlewareDisposition.Reject, Reason = reason };

    public static MiddlewareResult ShortCircuit(string response, string? reason = null) =>
        new()
        {
            Disposition = MiddlewareDisposition.ShortCircuit,
            ShortCircuitResponse = response,
            Reason = reason
        };
}

/// <summary>
/// How the pipeline should proceed after a middleware runs.
/// </summary>
public enum MiddlewareDisposition
{
    Continue = 0,
    Reject = 1,
    ShortCircuit = 2
}

/// <summary>
/// Lifecycle stages supported by the agent middleware pipeline.
/// </summary>
public enum MiddlewareStage
{
    BeforeAgent = 0,
    BeforeTool = 1,
    AfterTool = 2,
    AfterAgent = 3,
    BeforeResponse = 4
}

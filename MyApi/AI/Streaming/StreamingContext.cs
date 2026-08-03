namespace MyApi.AI.Streaming;

/// <summary>
/// Identifies who should receive a stream of execution events.
/// SignalR groups by <see cref="SessionId"/>; SSE subscribes by <see cref="CorrelationId"/>.
/// </summary>
public sealed class StreamingContext
{
    /// <summary>Per-request correlation id (also used as SSE subscription key).</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Conversation session; SignalR clients join <c>session:{SessionId}</c>.</summary>
    public string? SessionId { get; set; }

    /// <summary>Authenticated / logical user id.</summary>
    public string? UserId { get; set; }

    /// <summary>Optional SignalR connection id when known.</summary>
    public string? ConnectionId { get; set; }

    /// <summary>Workflow instance id when a workflow is active.</summary>
    public string? WorkflowId { get; set; }

    /// <summary>UTC start of the execution.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Extensible metadata (intent, plan id, etc.).</summary>
    public Dictionary<string, object?> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

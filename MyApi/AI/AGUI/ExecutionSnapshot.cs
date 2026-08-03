namespace MyApi.AI.AGUI;

/// <summary>
/// Immutable snapshot of one AI execution for dashboards / replay UIs.
/// </summary>
public sealed class ExecutionSnapshot
{
    public required string CorrelationId { get; init; }

    public string? SessionId { get; init; }

    public string? UserId { get; init; }

    public string? Intent { get; init; }

    public string? WorkflowId { get; init; }

    public string? SelectedAgentKey { get; init; }

    public AgentProgress? Progress { get; init; }

    public string? FinalResponse { get; init; }

    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public long DurationMs { get; init; }

    public IReadOnlyList<string> EventTimeline { get; init; } = Array.Empty<string>();
}

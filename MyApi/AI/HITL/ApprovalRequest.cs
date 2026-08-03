namespace MyApi.AI.HITL;

/// <summary>
/// Incoming request to create a human approval gate for a workflow instance.
/// </summary>
public sealed class ApprovalRequest
{
    public required Guid WorkflowInstanceId { get; init; }

    public required string WorkflowDefinitionId { get; init; }

    public required string CorrelationId { get; init; }

    public required string SessionId { get; init; }

    public required string UserId { get; init; }

    public required string Title { get; init; }

    public required string Description { get; init; }

    public Guid? CheckpointId { get; init; }

    /// <summary>Opaque business payload (e.g. employee id).</summary>
    public Dictionary<string, object?> Payload { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Optional expiration for future timeout / Hangfire jobs.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Future: parallel / multi-level approval group key.</summary>
    public string? ApprovalGroup { get; init; }

    public int RequiredApprovals { get; init; } = 1;
}

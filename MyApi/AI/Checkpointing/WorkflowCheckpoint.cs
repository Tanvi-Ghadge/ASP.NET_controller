namespace MyApi.AI.Checkpointing;

/// <summary>
/// Durable snapshot of a paused / suspended workflow instance.
/// </summary>
public sealed class WorkflowCheckpoint
{
    public Guid CheckpointId { get; init; } = Guid.NewGuid();

    /// <summary>Stable workflow instance id (resume key).</summary>
    public required Guid WorkflowInstanceId { get; init; }

    public required string WorkflowDefinitionId { get; init; }

    public required string WorkflowName { get; init; }

    /// <summary>Zero-based index of the next step to execute on resume.</summary>
    public int ResumeStepIndex { get; init; }

    /// <summary>Names of steps that already completed (must not re-run).</summary>
    public IReadOnlyList<string> CompletedSteps { get; init; } = Array.Empty<string>();

    /// <summary>Serialized workflow variables.</summary>
    public Dictionary<string, object?> Variables { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public required string SessionId { get; init; }

    public required string UserId { get; init; }

    public required string CorrelationId { get; init; }

    public required string CurrentMessage { get; init; }

    public string? SelectedAgentKey { get; init; }

    public string? MemoryJson { get; init; }

    public Dictionary<string, object?> ExecutionMetadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public required string Status { get; init; }

    public Guid? ApprovalRequestId { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

namespace MyApi.models.entities;

/// <summary>
/// EF entity: durable workflow checkpoint for HITL pause/resume.
/// </summary>
public class WorkflowCheckpointEntity
{
    public Guid CheckpointId { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    public required string WorkflowDefinitionId { get; set; }

    public required string WorkflowName { get; set; }

    public int ResumeStepIndex { get; set; }

    public required string CompletedStepsJson { get; set; }

    public required string VariablesJson { get; set; }

    public required string SessionId { get; set; }

    public required string UserId { get; set; }

    public required string CorrelationId { get; set; }

    public required string CurrentMessage { get; set; }

    public string? SelectedAgentKey { get; set; }

    public string? MemoryJson { get; set; }

    public string? ExecutionMetadataJson { get; set; }

    public required string Status { get; set; }

    public Guid? ApprovalRequestId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// EF entity: human approval request linked to a workflow instance.
/// </summary>
public class ApprovalRequestEntity
{
    public Guid ApprovalRequestId { get; set; }

    public Guid WorkflowInstanceId { get; set; }

    public required string WorkflowDefinitionId { get; set; }

    public required string CorrelationId { get; set; }

    public required string SessionId { get; set; }

    public required string UserId { get; set; }

    public required string Title { get; set; }

    public required string Description { get; set; }

    public int Status { get; set; }

    public Guid? CheckpointId { get; set; }

    public string? PayloadJson { get; set; }

    public string? ApprovalGroup { get; set; }

    public int RequiredApprovals { get; set; } = 1;

    public DateTimeOffset RequestedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? DecidedAt { get; set; }

    public string? DecidedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<ApprovalHistoryEntity> History { get; set; } = new List<ApprovalHistoryEntity>();
}

/// <summary>
/// EF entity: audit trail for approval decisions (supports multi-level / escalation later).
/// </summary>
public class ApprovalHistoryEntity
{
    public Guid HistoryId { get; set; }

    public Guid ApprovalRequestId { get; set; }

    public required string Action { get; set; }

    public required string Actor { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public ApprovalRequestEntity? ApprovalRequest { get; set; }
}

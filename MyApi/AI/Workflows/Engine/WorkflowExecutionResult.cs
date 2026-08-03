using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Engine;

/// <summary>
/// Engine-level result returned to the harness / resume service.
/// </summary>
public sealed class WorkflowExecutionResult
{
    public required string WorkflowId { get; init; }

    public required string WorkflowName { get; init; }

    public required WorkflowStatus Status { get; init; }

    public required string Output { get; init; }

    public object? Data { get; init; }

    public TimeSpan Duration { get; init; }

    public string CorrelationId { get; init; } = string.Empty;

    public string? Error { get; init; }

    public Guid? WorkflowInstanceId { get; init; }

    public Guid? ApprovalRequestId { get; init; }

    public Guid? CheckpointId { get; init; }

    public bool ApprovalPending => Status == WorkflowStatus.WaitingForApproval;

    public bool Succeeded => Status == WorkflowStatus.Completed;
}

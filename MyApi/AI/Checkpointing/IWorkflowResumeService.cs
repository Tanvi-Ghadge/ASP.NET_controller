using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Checkpointing;

/// <summary>
/// Resumes a durable workflow instance from its SQL checkpoint without restarting prior steps.
/// </summary>
public interface IWorkflowResumeService
{
    /// <summary>
    /// Loads checkpoint, rebuilds harness context, and continues from <see cref="WorkflowCheckpoint.ResumeStepIndex"/>.
    /// </summary>
    Task<WorkflowExecutionResult> ResumeAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>Workflow instance status for APIs.</summary>
    Task<WorkflowInstanceStatusDto?> GetStatusAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}

/// <summary>Public status projection for GET /api/workflows/{id}/status.</summary>
public sealed class WorkflowInstanceStatusDto
{
    public required Guid WorkflowInstanceId { get; init; }

    public required string WorkflowDefinitionId { get; init; }

    public required string WorkflowName { get; init; }

    public required string Status { get; init; }

    public int ResumeStepIndex { get; init; }

    public IReadOnlyList<string> CompletedSteps { get; init; } = Array.Empty<string>();

    public Guid? ApprovalRequestId { get; init; }

    public string? SessionId { get; init; }

    public string? UserId { get; init; }

    public string? CorrelationId { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }
}

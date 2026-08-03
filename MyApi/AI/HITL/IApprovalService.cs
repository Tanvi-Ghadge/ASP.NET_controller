namespace MyApi.AI.HITL;

/// <summary>
/// Port for creating and deciding human approvals. Independent of workflow engine internals.
/// </summary>
public interface IApprovalService
{
    /// <summary>Creates a pending approval and history entry.</summary>
    Task<ApprovalResponse> RequestAsync(ApprovalRequest request, CancellationToken cancellationToken = default);

    /// <summary>Approves a pending request for the given workflow instance.</summary>
    Task<ApprovalResponse> ApproveAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest decision,
        CancellationToken cancellationToken = default);

    /// <summary>Rejects a pending request for the given workflow instance.</summary>
    Task<ApprovalResponse> RejectAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest decision,
        CancellationToken cancellationToken = default);

    /// <summary>Lists pending approvals (optionally filtered by user).</summary>
    Task<IReadOnlyList<ApprovalResponse>> GetPendingAsync(
        string? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the latest approval for a workflow instance.</summary>
    Task<ApprovalResponse?> GetByWorkflowInstanceAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}

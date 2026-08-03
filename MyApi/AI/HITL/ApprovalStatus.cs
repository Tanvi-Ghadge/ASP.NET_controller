namespace MyApi.AI.HITL;

/// <summary>
/// Lifecycle of a human approval request.
/// </summary>
public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Expired = 3,
    Cancelled = 4,
    Escalated = 5
}

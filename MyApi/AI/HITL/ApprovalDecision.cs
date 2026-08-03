namespace MyApi.AI.HITL;

/// <summary>
/// Decision recorded when a human acts on an approval.
/// </summary>
public enum ApprovalDecision
{
    Approve = 0,
    Reject = 1,
    Escalate = 2,
    Defer = 3
}

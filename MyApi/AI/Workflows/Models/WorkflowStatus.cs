namespace MyApi.AI.Workflows.Models;

/// <summary>
/// Lifecycle status of a workflow instance.
/// </summary>
public enum WorkflowStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4,
    Skipped = 5,
    WaitingForApproval = 6,
    Paused = 7,
    Suspended = 8
}

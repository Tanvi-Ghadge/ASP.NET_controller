namespace MyApi.AI.Workflows.Models;

/// <summary>
/// Outcome of a single workflow step execution.
/// </summary>
public enum StepStatus
{
    Success = 0,
    Failure = 1,
    Retry = 2,
    Skip = 3,
    Cancelled = 4,
    /// <summary>Step created an approval gate; engine must pause and checkpoint.</summary>
    WaitingForApproval = 5
}

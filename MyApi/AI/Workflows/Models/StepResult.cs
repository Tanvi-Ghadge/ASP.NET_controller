namespace MyApi.AI.Workflows.Models;

/// <summary>
/// Result returned by <see cref="Steps.IWorkflowStep"/>.
/// </summary>
public sealed class StepResult
{
    public StepStatus Status { get; init; }

    public string? Message { get; init; }

    /// <summary>Optional output contributed by the step.</summary>
    public object? Output { get; init; }

    /// <summary>When Status is Retry, suggested delay before re-attempt (future use).</summary>
    public TimeSpan? RetryAfter { get; init; }

    public static StepResult Success(string? message = null, object? output = null) =>
        new() { Status = StepStatus.Success, Message = message, Output = output };

    public static StepResult Failure(string message) =>
        new() { Status = StepStatus.Failure, Message = message };

    public static StepResult Skip(string? message = null) =>
        new() { Status = StepStatus.Skip, Message = message };

    public static StepResult Cancelled(string? message = null) =>
        new() { Status = StepStatus.Cancelled, Message = message };

    public static StepResult Retry(string message, TimeSpan? retryAfter = null) =>
        new() { Status = StepStatus.Retry, Message = message, RetryAfter = retryAfter };

    /// <summary>Signals the engine to pause, persist a checkpoint, and wait for HITL.</summary>
    public static StepResult WaitingForApproval(string? message = null, object? output = null) =>
        new() { Status = StepStatus.WaitingForApproval, Message = message, Output = output };
}

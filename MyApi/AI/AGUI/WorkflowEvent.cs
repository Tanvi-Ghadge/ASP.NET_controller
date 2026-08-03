namespace MyApi.AI.AGUI;

/// <summary>
/// Workflow-centric helpers for AG-UI workflow lifecycle events.
/// </summary>
public static class WorkflowEvent
{
    public static WorkflowStartedEvent Started(
        string correlationId,
        string workflowId,
        string workflowName,
        int stepCount) =>
        new()
        {
            CorrelationId = correlationId,
            WorkflowId = workflowId,
            WorkflowName = workflowName,
            StepCount = stepCount
        };

    public static WorkflowCompletedEvent Completed(
        string correlationId,
        string workflowId,
        string workflowName,
        string status,
        long durationMs) =>
        new()
        {
            CorrelationId = correlationId,
            WorkflowId = workflowId,
            WorkflowName = workflowName,
            Status = status,
            DurationMs = durationMs
        };

    public static WorkflowStepStartedEvent StepStarted(
        string correlationId,
        string workflowId,
        string stepName,
        int stepIndex) =>
        new()
        {
            CorrelationId = correlationId,
            WorkflowId = workflowId,
            StepName = stepName,
            StepIndex = stepIndex
        };

    public static WorkflowStepCompletedEvent StepCompleted(
        string correlationId,
        string workflowId,
        string stepName,
        long durationMs,
        string? message = null) =>
        new()
        {
            CorrelationId = correlationId,
            WorkflowId = workflowId,
            StepName = stepName,
            DurationMs = durationMs,
            Message = message
        };

    public static WorkflowStepFailedEvent StepFailed(
        string correlationId,
        string workflowId,
        string stepName,
        string error,
        long durationMs) =>
        new()
        {
            CorrelationId = correlationId,
            WorkflowId = workflowId,
            StepName = stepName,
            Error = error,
            DurationMs = durationMs
        };

    public static WorkflowStepSkippedEvent StepSkipped(
        string correlationId,
        string workflowId,
        string stepName,
        string? reason = null) =>
        new()
        {
            CorrelationId = correlationId,
            WorkflowId = workflowId,
            StepName = stepName,
            Reason = reason
        };
}

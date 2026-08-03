namespace MyApi.AI.Orchestration;

/// <summary>
/// Final result returned from the orchestrator to the harness.
/// </summary>
public sealed class OrchestrationResult
{
    /// <summary>Plan that was executed.</summary>
    public required OrchestrationPlan Plan { get; init; }

    /// <summary>User-facing response text.</summary>
    public required string Output { get; init; }

    /// <summary>True when the selected workflow(s) completed successfully.</summary>
    public bool Succeeded { get; init; }

    /// <summary>True when the workflow paused for human approval (not a failure).</summary>
    public bool ApprovalPending { get; init; }

    /// <summary>Workflow instance id when approval is pending or completed.</summary>
    public Guid? WorkflowInstanceId { get; init; }

    /// <summary>Approval request id when HITL was triggered.</summary>
    public Guid? ApprovalRequestId { get; init; }

    /// <summary>Error detail when <see cref="Succeeded"/> is false and not approval-pending.</summary>
    public string? Error { get; init; }

    /// <summary>Wall-clock duration of orchestration + execution.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Correlation id.</summary>
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>Optional structured payload from the workflow engine.</summary>
    public object? Data { get; init; }
}

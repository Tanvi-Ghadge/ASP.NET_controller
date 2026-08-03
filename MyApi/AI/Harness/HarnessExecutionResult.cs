namespace MyApi.AI.Harness;

/// <summary>
/// Outcome of a harness execution, including response payload and telemetry fields.
/// </summary>
public sealed class HarnessExecutionResult
{
    /// <summary>Conversation session id.</summary>
    public required string SessionId { get; init; }

    /// <summary>Assistant reply text.</summary>
    public required string Response { get; init; }

    /// <summary>Correlation id for this run.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Resolved user id.</summary>
    public required string UserId { get; init; }

    /// <summary>Number of long-term memories injected into the prompt.</summary>
    public int MemoriesRetrieved { get; init; }

    /// <summary>Wall-clock duration of the full harness pipeline.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>UTC finish timestamp.</summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>True when the run completed without throwing.</summary>
    public bool Succeeded { get; init; } = true;

    /// <summary>True when workflow is paused awaiting human approval.</summary>
    public bool ApprovalPending { get; init; }

    /// <summary>Workflow instance id for approve/reject APIs.</summary>
    public Guid? WorkflowInstanceId { get; init; }

    /// <summary>Pending approval request id.</summary>
    public Guid? ApprovalRequestId { get; init; }

    /// <summary>Optional error summary when <see cref="Succeeded"/> is false.</summary>
    public string? Error { get; init; }
}

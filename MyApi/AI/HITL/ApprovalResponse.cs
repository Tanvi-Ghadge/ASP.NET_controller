namespace MyApi.AI.HITL;

/// <summary>
/// Result returned after creating or deciding an approval.
/// </summary>
public sealed class ApprovalResponse
{
    public required Guid ApprovalRequestId { get; init; }

    public required Guid WorkflowInstanceId { get; init; }

    public required ApprovalStatus Status { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? DecidedBy { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset RequestedAt { get; init; }

    public DateTimeOffset? DecidedAt { get; init; }

    public Dictionary<string, object?> Payload { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Payload for approve / reject API calls.
/// </summary>
public sealed class ApprovalDecisionRequest
{
    /// <summary>Actor performing the decision (manager id / email).</summary>
    public string? DecidedBy { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }
}

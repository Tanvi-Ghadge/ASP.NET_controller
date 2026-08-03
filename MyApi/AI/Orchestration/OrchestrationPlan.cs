namespace MyApi.AI.Orchestration;

/// <summary>
/// Decision output of the orchestrator before execution.
/// Describes what should run — not how business data is mutated.
/// </summary>
public sealed class OrchestrationPlan
{
    /// <summary>Unique plan id for this decision.</summary>
    public Guid PlanId { get; init; } = Guid.NewGuid();

    /// <summary>Intent detected by the classifier.</summary>
    public required UserIntent DetectedIntent { get; init; }

    /// <summary>Workflow definition id to execute.</summary>
    public required string SelectedWorkflowId { get; init; }

    /// <summary>Friendly workflow name.</summary>
    public required string SelectedWorkflowName { get; init; }

    /// <summary>Logical agent key selected for this plan.</summary>
    public required string SelectedAgentKey { get; init; }

    /// <summary>Execution strategy (sequential today).</summary>
    public ExecutionStrategy ExecutionStrategy { get; init; } = ExecutionStrategy.Sequential;

    /// <summary>Ordered workflow ids when multiple are required (future parallel/chained plans).</summary>
    public IReadOnlyList<string> WorkflowIds { get; init; } = Array.Empty<string>();

    /// <summary>Estimated number of steps (informational).</summary>
    public int EstimatedSteps { get; init; }

    /// <summary>Human-readable rationale for logs / future audit.</summary>
    public string? Rationale { get; init; }

    /// <summary>Extensible plan metadata (plugins needed, approval flags, etc.).</summary>
    public Dictionary<string, object?> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>UTC when the plan was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

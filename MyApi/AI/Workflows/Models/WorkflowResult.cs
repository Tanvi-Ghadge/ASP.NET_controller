namespace MyApi.AI.Workflows.Models;

/// <summary>
/// Final business result produced by a workflow.
/// </summary>
public sealed class WorkflowResult
{
    /// <summary>User-facing response text.</summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>Optional structured payload for future clients.</summary>
    public object? Data { get; init; }

    /// <summary>Definition id that produced this result.</summary>
    public string WorkflowId { get; init; } = string.Empty;

    /// <summary>Friendly workflow name.</summary>
    public string WorkflowName { get; init; } = string.Empty;
}

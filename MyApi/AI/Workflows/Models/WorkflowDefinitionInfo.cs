namespace MyApi.AI.Workflows.Models;

/// <summary>
/// Static metadata describing a workflow definition (not a running instance).
/// </summary>
public sealed class WorkflowDefinitionInfo
{
    public required string WorkflowId { get; init; }

    public required string WorkflowName { get; init; }

    public string Description { get; init; } = string.Empty;
}

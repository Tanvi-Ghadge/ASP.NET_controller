using MyApi.AI.Workflows.Steps;

namespace MyApi.AI.Workflows.Models;

/// <summary>
/// Contract for a reusable workflow definition composed of ordered steps.
/// </summary>
public interface IWorkflowDefinition
{
    /// <summary>Stable definition identifier.</summary>
    string WorkflowId { get; }

    /// <summary>Human-readable name.</summary>
    string WorkflowName { get; }

    /// <summary>What this workflow accomplishes.</summary>
    string Description { get; }

    /// <summary>Ordered replaceable steps.</summary>
    IReadOnlyList<IWorkflowStep> Steps { get; }
}

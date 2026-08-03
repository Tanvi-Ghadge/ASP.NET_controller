using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// One replaceable business operation inside a workflow.
/// </summary>
public interface IWorkflowStep
{
    /// <summary>Stable step name for logging and future graph edges.</summary>
    string Name { get; }

    /// <summary>Executes the step against the shared context.</summary>
    Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default);
}

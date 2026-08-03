using MyApi.AI.Workflows.Definitions;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Orchestration;

/// <summary>
/// Maps a <see cref="UserIntent"/> to a workflow definition id.
/// Kept outside the orchestrator so routing tables can evolve independently.
/// </summary>
public interface IWorkflowSelector
{
    /// <summary>
    /// Selects the workflow definition for the given intent.
    /// </summary>
    WorkflowSelection Select(UserIntent intent);
}

/// <summary>
/// Result of workflow selection.
/// </summary>
public sealed class WorkflowSelection
{
    public required string WorkflowId { get; init; }

    public required string WorkflowName { get; init; }

    public int EstimatedSteps { get; init; }
}

/// <summary>
/// Default intent → workflow mapping using registered <see cref="IWorkflowDefinition"/> metadata.
/// </summary>
public sealed class WorkflowSelector : IWorkflowSelector
{
    private readonly IReadOnlyDictionary<string, IWorkflowDefinition> _definitions;
    private readonly ILogger<WorkflowSelector> _logger;

    public WorkflowSelector(
        IEnumerable<IWorkflowDefinition> definitions,
        ILogger<WorkflowSelector> logger)
    {
        _definitions = definitions.ToDictionary(d => d.WorkflowId, StringComparer.OrdinalIgnoreCase);
        _logger = logger;
    }

    /// <inheritdoc />
    public WorkflowSelection Select(UserIntent intent)
    {
        var workflowId = intent switch
        {
            UserIntent.EmployeeReport => EmployeeReportWorkflow.Id,
            UserIntent.DeleteEmployee => DeleteEmployeeWorkflow.Id,
            UserIntent.EmployeeLookup => EmployeeLookupWorkflow.Id,
            UserIntent.PayrollReport => PayrollReportWorkflow.Id,
            UserIntent.EmployeePayrollReport => GenerateEmployeePayrollReportWorkflow.Id,
            UserIntent.GeneralConversation => EmployeeLookupWorkflow.Id,
            _ => EmployeeLookupWorkflow.Id
        };

        if (!_definitions.TryGetValue(workflowId, out var definition))
        {
            throw new InvalidOperationException(
                $"Workflow '{workflowId}' selected for intent {intent} is not registered.");
        }

        var selection = new WorkflowSelection
        {
            WorkflowId = definition.WorkflowId,
            WorkflowName = definition.WorkflowName,
            EstimatedSteps = definition.Steps.Count
        };

        _logger.LogInformation(
            "Workflow selected. Intent={Intent}, WorkflowId={WorkflowId}, WorkflowName={WorkflowName}, EstimatedSteps={EstimatedSteps}",
            intent,
            selection.WorkflowId,
            selection.WorkflowName,
            selection.EstimatedSteps);

        return selection;
    }
}

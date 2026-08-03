using MyApi.AI.Workflows.Models;
using MyApi.AI.Workflows.Steps;

namespace MyApi.AI.Workflows.Definitions;

/// <summary>
/// Business process: look up one employee and return a formatted result.
/// Steps: Load Employee → Format Employee → Return Result (via FinalResponse).
/// </summary>
public sealed class EmployeeLookupWorkflow : IWorkflowDefinition
{
    public const string Id = "employee-lookup";

    private readonly IReadOnlyList<IWorkflowStep> _steps;

    public EmployeeLookupWorkflow(
        EmployeeLookupStep lookupStep,
        EmployeeSummaryStep summaryStep)
    {
        _steps = [lookupStep, summaryStep];
    }

    /// <inheritdoc />
    public string WorkflowId => Id;

    /// <inheritdoc />
    public string WorkflowName => "Employee Lookup Workflow";

    /// <inheritdoc />
    public string Description =>
        "Loads a single employee by id, formats the record, and returns the result.";

    /// <inheritdoc />
    public IReadOnlyList<IWorkflowStep> Steps => _steps;
}

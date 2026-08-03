using MyApi.AI.Workflows.Models;
using MyApi.AI.Workflows.Steps;

namespace MyApi.AI.Workflows.Definitions;

/// <summary>
/// Delete-employee business process with HITL approval before mutation.
/// Validate → Request Approval → (pause) → Delete → Notify → Format.
/// </summary>
public sealed class DeleteEmployeeWorkflow : IWorkflowDefinition
{
    public const string Id = "employee-delete";

    private readonly IReadOnlyList<IWorkflowStep> _steps;

    public DeleteEmployeeWorkflow(
        ValidateEmployeeDeleteStep validateStep,
        RequestDeleteApprovalStep approvalStep,
        DeleteEmployeeStep deleteStep,
        NotifyDeleteStep notifyStep,
        EmployeeSummaryStep summaryStep)
    {
        _steps = [validateStep, approvalStep, deleteStep, notifyStep, summaryStep];
    }

    /// <inheritdoc />
    public string WorkflowId => Id;

    /// <inheritdoc />
    public string WorkflowName => "Delete Employee Workflow";

    /// <inheritdoc />
    public string Description =>
        "Validates the employee, requests manager approval, pauses for HITL, then deletes and notifies.";

    /// <inheritdoc />
    public IReadOnlyList<IWorkflowStep> Steps => _steps;
}

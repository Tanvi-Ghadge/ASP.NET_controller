using MyApi.AI.Workflows.Models;
using MyApi.AI.Workflows.Steps;

namespace MyApi.AI.Workflows.Definitions;

/// <summary>
/// Business process: generate an employee report.
/// Steps: Load + Statistics + Summary + Report (ReportGenerationStep) → Format → Email (optional/skip).
/// </summary>
public sealed class EmployeeReportWorkflow : IWorkflowDefinition
{
    public const string Id = "employee-report";

    private readonly IReadOnlyList<IWorkflowStep> _steps;

    public EmployeeReportWorkflow(
        ReportGenerationStep reportGenerationStep,
        EmployeeSummaryStep summaryStep,
        EmailReportStep emailReportStep)
    {
        _steps = [reportGenerationStep, summaryStep, emailReportStep];
    }

    /// <inheritdoc />
    public string WorkflowId => Id;

    /// <inheritdoc />
    public string WorkflowName => "Employee Report Workflow";

    /// <inheritdoc />
    public string Description =>
        "Loads employees, calculates statistics, generates a text report, and returns the response.";

    /// <inheritdoc />
    public IReadOnlyList<IWorkflowStep> Steps => _steps;
}

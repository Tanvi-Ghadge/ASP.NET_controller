using MyApi.AI.Workflows.Models;
using MyApi.AI.Workflows.Steps;

namespace MyApi.AI.Workflows.Definitions;

/// <summary>
/// Multi-agent payroll report: Employee → Payroll → Reporting → (optional) Notification via A2A.
/// </summary>
public sealed class GenerateEmployeePayrollReportWorkflow : IWorkflowDefinition
{
    public const string Id = "generate-employee-payroll-report";

    private readonly IReadOnlyList<IWorkflowStep> _steps;

    public GenerateEmployeePayrollReportWorkflow(
        LoadEmployeeA2AStep loadEmployee,
        LoadPayrollA2AStep loadPayroll,
        GeneratePayrollReportA2AStep generateReport,
        NotifyReportA2AStep notifyReport)
    {
        _steps = [loadEmployee, loadPayroll, generateReport, notifyReport];
    }

    /// <inheritdoc />
    public string WorkflowId => Id;

    /// <inheritdoc />
    public string WorkflowName => "Generate Employee Payroll Report";

    /// <inheritdoc />
    public string Description =>
        "Collaborative A2A workflow: load employee, load payroll, generate report, optionally email HR.";

    /// <inheritdoc />
    public IReadOnlyList<IWorkflowStep> Steps => _steps;
}

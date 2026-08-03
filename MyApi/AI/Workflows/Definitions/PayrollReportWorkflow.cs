using MyApi.AI.Workflows.Models;
using MyApi.AI.Workflows.Steps;

namespace MyApi.AI.Workflows.Definitions;

/// <summary>
/// Payroll-focused report workflow: PayrollAgent → ReportingAgent → optional NotificationAgent.
/// </summary>
public sealed class PayrollReportWorkflow : IWorkflowDefinition
{
    public const string Id = "payroll-report";

    private readonly IReadOnlyList<IWorkflowStep> _steps;

    public PayrollReportWorkflow(
        LoadPayrollA2AStep loadPayroll,
        GeneratePayrollReportA2AStep generateReport,
        NotifyReportA2AStep notifyReport)
    {
        _steps = [loadPayroll, generateReport, notifyReport];
    }

    /// <inheritdoc />
    public string WorkflowId => Id;

    /// <inheritdoc />
    public string WorkflowName => "Payroll Report Workflow";

    /// <inheritdoc />
    public string Description =>
        "Loads compensation/payroll via PayrollAgent, formats via ReportingAgent, optionally notifies.";

    /// <inheritdoc />
    public IReadOnlyList<IWorkflowStep> Steps => _steps;
}

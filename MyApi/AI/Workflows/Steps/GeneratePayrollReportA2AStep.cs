using MyApi.AI.A2A;
using MyApi.AI.Agents.Reporting;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Asks <see cref="ReportingAgent"/> to generate a payroll/compensation report via A2A.
/// </summary>
public sealed class GeneratePayrollReportA2AStep : IWorkflowStep
{
    public const string ReportTextKey = "ReportText";

    private readonly IAgentCommunication _a2a;
    private readonly ILogger<GeneratePayrollReportA2AStep> _logger;

    public GeneratePayrollReportA2AStep(
        IAgentCommunication a2a,
        ILogger<GeneratePayrollReportA2AStep> logger)
    {
        _a2a = a2a;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "GeneratePayrollReportA2A";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var employeeJson = context.State.Get<string>(LoadEmployeeA2AStep.EmployeeJsonKey);
        var payrollJson = context.State.Get<string>(LoadPayrollA2AStep.PayrollJsonKey);
        var compensationJson = context.State.Get<string>(LoadPayrollA2AStep.CompensationJsonKey);
        var employeeId = context.State.Get<int?>(LoadEmployeeA2AStep.EmployeeIdKey);

        string operation;
        Dictionary<string, object?> data;

        if (!string.IsNullOrWhiteSpace(employeeJson) && !string.IsNullOrWhiteSpace(payrollJson))
        {
            operation = "GeneratePayrollReport";
            data = new Dictionary<string, object?>
            {
                ["employeeJson"] = employeeJson,
                ["payrollJson"] = payrollJson
            };
        }
        else if (employeeId is int id)
        {
            // ReportingAgent may further A2A to PayrollAgent if needed.
            operation = "GeneratePayrollReport";
            data = new Dictionary<string, object?> { ["employeeId"] = id };
        }
        else
        {
            operation = "GenerateCompensationReport";
            data = new Dictionary<string, object?>
            {
                ["compensationJson"] = compensationJson ?? payrollJson
            };
        }

        var response = await _a2a.SendAsync(
            new AgentRequest
            {
                AgentName = ReportingAgent.Key,
                Operation = operation,
                CorrelationId = context.CorrelationId,
                SessionId = context.Session.SessionId,
                MemoryContext = context.Memory,
                RequestData = data,
                ExecutionMetadata =
                {
                    ["WorkflowId"] = context.State.WorkflowId,
                    ["Step"] = Name
                }
            },
            cancellationToken);

        if (!response.Succeeded)
        {
            var error = response.Errors.FirstOrDefault() ?? "ReportingAgent failed.";
            context.State.Set("FinalResponse", error);
            return StepResult.Failure(error);
        }

        var report = response.TextOutput ?? response.Payload?.ToString() ?? string.Empty;
        context.State.Set(ReportTextKey, report);
        context.State.Set("FinalResponse", report);

        _logger.LogInformation(
            "Payroll report generated via A2A. Length={Length}, CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
            report.Length,
            context.CorrelationId,
            context.State.WorkflowId);

        return StepResult.Success("Generated payroll report via ReportingAgent.", report);
    }
}

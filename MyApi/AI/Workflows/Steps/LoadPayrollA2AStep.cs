using System.Text.Json;
using MyApi.AI.A2A;
using MyApi.AI.Agents.Payroll;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Loads payroll via A2A to <see cref="PayrollAgent"/>.
/// </summary>
public sealed class LoadPayrollA2AStep : IWorkflowStep
{
    public const string PayrollJsonKey = "PayrollJson";
    public const string CompensationJsonKey = "CompensationJson";

    private readonly IAgentCommunication _a2a;
    private readonly ILogger<LoadPayrollA2AStep> _logger;

    public LoadPayrollA2AStep(IAgentCommunication a2a, ILogger<LoadPayrollA2AStep> logger)
    {
        _a2a = a2a;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "LoadPayrollA2A";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var employeeId = context.State.Get<int?>(LoadEmployeeA2AStep.EmployeeIdKey)
            ?? context.State.Get<int?>(EmployeeLookupStep.EmployeeIdVariableKey);

        if (employeeId is int id)
        {
            var response = await _a2a.SendAsync(
                new AgentRequest
                {
                    AgentName = PayrollAgent.Key,
                    Operation = "GetPayroll",
                    CorrelationId = context.CorrelationId,
                    SessionId = context.Session.SessionId,
                    MemoryContext = context.Memory,
                    RequestData = new Dictionary<string, object?> { ["employeeId"] = id },
                    ExecutionMetadata =
                    {
                        ["WorkflowId"] = context.State.WorkflowId,
                        ["Step"] = Name
                    }
                },
                cancellationToken);

            if (!response.Succeeded)
            {
                var error = response.Errors.FirstOrDefault() ?? "PayrollAgent failed.";
                context.State.Set("FinalResponse", error);
                return StepResult.Failure(error);
            }

            var json = response.TextOutput ?? JsonSerializer.Serialize(response.Payload);
            context.State.Set(PayrollJsonKey, json);

            _logger.LogInformation(
                "Payroll loaded via A2A. EmployeeId={EmployeeId}, CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
                id,
                context.CorrelationId,
                context.State.WorkflowId);

            return StepResult.Success($"Loaded payroll for employee {id} via PayrollAgent.", json);
        }

        var summaryResponse = await _a2a.SendAsync(
            new AgentRequest
            {
                AgentName = PayrollAgent.Key,
                Operation = "GetCompensationSummary",
                CorrelationId = context.CorrelationId,
                SessionId = context.Session.SessionId,
                MemoryContext = context.Memory,
                ExecutionMetadata =
                {
                    ["WorkflowId"] = context.State.WorkflowId,
                    ["Step"] = Name
                }
            },
            cancellationToken);

        if (!summaryResponse.Succeeded)
        {
            var error = summaryResponse.Errors.FirstOrDefault() ?? "PayrollAgent failed.";
            context.State.Set("FinalResponse", error);
            return StepResult.Failure(error);
        }

        var summaryJson = summaryResponse.TextOutput ?? JsonSerializer.Serialize(summaryResponse.Payload);
        context.State.Set(CompensationJsonKey, summaryJson);
        context.State.Set(PayrollJsonKey, summaryJson);

        _logger.LogInformation(
            "Compensation summary loaded via A2A. CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
            context.CorrelationId,
            context.State.WorkflowId);

        return StepResult.Success("Loaded compensation summary via PayrollAgent.", summaryJson);
    }
}

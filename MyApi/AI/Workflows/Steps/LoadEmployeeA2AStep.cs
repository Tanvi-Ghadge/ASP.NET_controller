using System.Text.Json;
using System.Text.RegularExpressions;
using MyApi.AI.A2A;
using MyApi.AI.Agents.Employee;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Loads an employee via A2A to <see cref="EmployeeAgent"/> — never calls employee services directly.
/// </summary>
public sealed partial class LoadEmployeeA2AStep : IWorkflowStep
{
    public const string EmployeeJsonKey = "EmployeeJson";
    public const string EmployeeIdKey = "EmployeeId";

    private readonly IAgentCommunication _a2a;
    private readonly ILogger<LoadEmployeeA2AStep> _logger;

    public LoadEmployeeA2AStep(IAgentCommunication a2a, ILogger<LoadEmployeeA2AStep> logger)
    {
        _a2a = a2a;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "LoadEmployeeA2A";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var match = EmployeeIdRegex().Match(context.Harness.CurrentMessage);
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var employeeId))
        {
            // Multi-agent payroll report can proceed without a specific id when summarizing all payroll.
            _logger.LogInformation(
                "No employee id in message; skipping employee A2A load. CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
                context.CorrelationId,
                context.State.WorkflowId);
            return StepResult.Skip("No employee id present; continuing without employee record.");
        }

        var response = await _a2a.SendAsync(
            new AgentRequest
            {
                AgentName = EmployeeAgent.Key,
                Operation = "GetEmployee",
                CorrelationId = context.CorrelationId,
                SessionId = context.Session.SessionId,
                MemoryContext = context.Memory,
                RequestData = new Dictionary<string, object?> { ["employeeId"] = employeeId },
                ExecutionMetadata =
                {
                    ["WorkflowId"] = context.State.WorkflowId,
                    ["Step"] = Name
                }
            },
            cancellationToken);

        if (!response.Succeeded)
        {
            var error = response.Errors.FirstOrDefault() ?? "EmployeeAgent failed.";
            context.State.Set("FinalResponse", error);
            return StepResult.Failure(error);
        }

        var json = response.TextOutput ?? JsonSerializer.Serialize(response.Payload);
        context.State.Set(EmployeeIdKey, employeeId);
        context.State.Set(EmployeeJsonKey, json);
        context.State.Set(EmployeeLookupStep.EmployeeIdVariableKey, employeeId);

        _logger.LogInformation(
            "Employee loaded via A2A. EmployeeId={EmployeeId}, CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
            employeeId,
            context.CorrelationId,
            context.State.WorkflowId);

        return StepResult.Success($"Loaded employee {employeeId} via EmployeeAgent.", json);
    }

    [GeneratedRegex(
        @"\bemployee\s+(?<id>\d+)\b|\bid\s*(?:#|=|:)?\s*(?<id>\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmployeeIdRegex();
}

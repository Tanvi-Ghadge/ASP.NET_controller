using System.Text;
using MyApi.AI.AgentRouter;
using MyApi.AI.Agents.Employee;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;
using MyApi.DTO.Employee;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Formats employee (or report) data into a user-facing response.
/// Falls back to the router-selected domain agent when no structured data was produced.
/// </summary>
public sealed class EmployeeSummaryStep : IWorkflowStep
{
    private readonly IAgentRouter _agentRouter;
    private readonly ILogger<EmployeeSummaryStep> _logger;

    public EmployeeSummaryStep(
        IAgentRouter agentRouter,
        ILogger<EmployeeSummaryStep> logger)
    {
        _agentRouter = agentRouter;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "FormatEmployee";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.State.Get<string>("FinalResponse") is { Length: > 0 } existingFinal)
        {
            return StepResult.Success("Final response already set.", existingFinal);
        }

        if (context.State.Get<Reademployeedto>(EmployeeLookupStep.EmployeeVariableKey) is { } employee)
        {
            var formatted = FormatEmployee(employee);
            context.State.Set("EmployeeSummary", formatted);
            context.State.Set("FinalResponse", formatted);
            _logger.LogInformation(
                "Formatted single employee summary. CorrelationId={CorrelationId}",
                context.CorrelationId);
            return StepResult.Success("Formatted employee.", formatted);
        }

        if (context.State.Get<string>("ReportText") is { Length: > 0 } report)
        {
            context.State.Set("EmployeeSummary", report);
            context.State.Set("FinalResponse", report);
            return StepResult.Success("Used generated report as summary.", report);
        }

        var agentKey = ResolveAgentKey(context);
        var agent = _agentRouter.Resolve(agentKey);

        _logger.LogInformation(
            "No structured workflow data; invoking agent via router. AgentKey={AgentKey}, CorrelationId={CorrelationId}",
            agent.AgentKey,
            context.CorrelationId);

        var agentReply = await agent.RunAsync(context.Harness);
        context.State.Set("AgentResponse", agentReply);
        context.State.Set("FinalResponse", agentReply);
        return StepResult.Success("Agent produced conversational response.", agentReply);
    }

    private static string ResolveAgentKey(WorkflowExecutionContext context)
    {
        if (context.Metadata.TryGetValue("SelectedAgentKey", out var value) &&
            value is string key &&
            !string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        return EmployeeAgent.Key;
    }

    private static string FormatEmployee(Reademployeedto employee)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Employee {employee.Id}: {employee.Name}");
        sb.AppendLine($"Email: {employee.Email}");
        sb.AppendLine($"Department: {employee.DepartmentName}");
        sb.AppendLine($"Salary: {employee.Salary:C}");
        if (!string.IsNullOrWhiteSpace(employee.ManagerName))
        {
            sb.AppendLine($"Manager: {employee.ManagerName}");
        }

        return sb.ToString().TrimEnd();
    }
}

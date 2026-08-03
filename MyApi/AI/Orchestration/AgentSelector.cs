using MyApi.AI.Agents.Employee;
using MyApi.AI.Agents.Payroll;
using MyApi.AI.Agents.Reporting;
using MyApi.AI.Workflows.Definitions;

namespace MyApi.AI.Orchestration;

/// <summary>
/// Selects which logical agent should participate in a plan.
/// Orchestrator resolves the concrete agent through <c>IAgentRouter</c>, never by type.
/// </summary>
public interface IAgentSelector
{
    /// <summary>Selects an agent key for the given intent and optional workflow id.</summary>
    AgentSelection Select(UserIntent intent, string workflowId);
}

/// <summary>
/// Result of agent selection.
/// </summary>
public sealed class AgentSelection
{
    /// <summary>Stable agent key (e.g. employee-agent).</summary>
    public required string AgentKey { get; init; }

    /// <summary>Display name.</summary>
    public required string AgentName { get; init; }
}

/// <summary>
/// Maps intents / workflows to primary domain agent keys.
/// </summary>
public sealed class AgentSelector : IAgentSelector
{
    private readonly ILogger<AgentSelector> _logger;

    public AgentSelector(ILogger<AgentSelector> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public AgentSelection Select(UserIntent intent, string workflowId)
    {
        var selection = intent switch
        {
            UserIntent.PayrollReport => new AgentSelection
            {
                AgentKey = PayrollAgent.Key,
                AgentName = "PayrollAgent"
            },
            UserIntent.EmployeePayrollReport => new AgentSelection
            {
                AgentKey = EmployeeAgent.Key,
                AgentName = "EmployeeAgent"
            },
            UserIntent.EmployeeReport => new AgentSelection
            {
                AgentKey = ReportingAgent.Key,
                AgentName = "ReportingAgent"
            },
            _ when string.Equals(workflowId, PayrollReportWorkflow.Id, StringComparison.OrdinalIgnoreCase) =>
                new AgentSelection { AgentKey = PayrollAgent.Key, AgentName = "PayrollAgent" },
            _ when string.Equals(workflowId, GenerateEmployeePayrollReportWorkflow.Id, StringComparison.OrdinalIgnoreCase) =>
                new AgentSelection { AgentKey = EmployeeAgent.Key, AgentName = "EmployeeAgent" },
            _ => new AgentSelection
            {
                AgentKey = EmployeeAgent.Key,
                AgentName = "EmployeeAgent"
            }
        };

        _logger.LogInformation(
            "Agent selected. Intent={Intent}, WorkflowId={WorkflowId}, AgentKey={AgentKey}",
            intent,
            workflowId,
            selection.AgentKey);

        return selection;
    }
}

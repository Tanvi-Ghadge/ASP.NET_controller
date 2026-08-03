using MyApi.AI.AgentRouter;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Runs the orchestrator-selected domain agent conversationally via <see cref="IAgentRouter"/>.
/// Used when structured workflow data is unavailable (general / agent-first paths).
/// </summary>
public sealed class RunSelectedAgentStep : IWorkflowStep
{
    private readonly IAgentRouter _agentRouter;
    private readonly ILogger<RunSelectedAgentStep> _logger;

    public RunSelectedAgentStep(IAgentRouter agentRouter, ILogger<RunSelectedAgentStep> logger)
    {
        _agentRouter = agentRouter;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "RunSelectedAgent";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        if (context.State.Get<string>("FinalResponse") is { Length: > 0 } existing)
        {
            return StepResult.Success("Final response already set.", existing);
        }

        var agentKey = ResolveAgentKey(context);
        var agent = _agentRouter.Resolve(agentKey);

        _logger.LogInformation(
            "Agent Selected for conversational run. AgentKey={AgentKey}, AgentName={AgentName}, CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
            agent.AgentKey,
            agent.AgentName,
            context.CorrelationId,
            context.State.WorkflowId);

        var reply = await agent.RunAsync(context.Harness);
        context.State.Set("AgentResponse", reply);
        context.State.Set("FinalResponse", reply);
        return StepResult.Success($"Agent {agent.AgentName} produced conversational response.", reply);
    }

    private static string ResolveAgentKey(WorkflowExecutionContext context)
    {
        if (context.Metadata.TryGetValue("SelectedAgentKey", out var value) &&
            value is string key &&
            !string.IsNullOrWhiteSpace(key))
        {
            return key;
        }

        return Agents.Employee.EmployeeAgent.Key;
    }
}

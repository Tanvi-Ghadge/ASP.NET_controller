using MyApi.AI.Agents;

namespace MyApi.AI.AgentRouter;

/// <summary>
/// Default in-process agent router backed by DI-registered <see cref="IDomainAgent"/> instances.
/// </summary>
public sealed class AgentRouter : IAgentRouter
{
    private readonly IReadOnlyDictionary<string, IDomainAgent> _agents;
    private readonly ILogger<AgentRouter> _logger;

    public AgentRouter(IEnumerable<IDomainAgent> agents, ILogger<AgentRouter> logger)
    {
        _logger = logger;
        _agents = agents.ToDictionary(a => a.AgentKey, StringComparer.OrdinalIgnoreCase);
        RegisteredAgentKeys = _agents.Keys.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> RegisteredAgentKeys { get; }

    /// <inheritdoc />
    public IDomainAgent Resolve(string agentKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);

        if (!_agents.TryGetValue(agentKey, out var agent))
        {
            throw new InvalidOperationException(
                $"Agent '{agentKey}' is not registered. Known: {string.Join(", ", RegisteredAgentKeys)}");
        }

        _logger.LogInformation(
            "Agent Selected. AgentKey={AgentKey}, AgentName={AgentName}, Domain={Domain}",
            agent.AgentKey,
            agent.AgentName,
            agent.Domain);

        return agent;
    }
}

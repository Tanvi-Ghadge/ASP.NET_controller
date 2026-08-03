using MyApi.AI.Agents;

namespace MyApi.AI.AgentRouter;

/// <summary>
/// Resolves concrete domain agents by key without exposing implementations to the orchestrator.
/// </summary>
public interface IAgentRouter
{
    /// <summary>Resolves a registered domain agent.</summary>
    IDomainAgent Resolve(string agentKey);

    /// <summary>Returns all registered agent keys.</summary>
    IReadOnlyCollection<string> RegisteredAgentKeys { get; }
}

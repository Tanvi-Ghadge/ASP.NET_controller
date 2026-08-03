using AgentResponse = MyApi.AI.A2A.AgentResponse;

namespace MyApi.AI.A2A;

/// <summary>
/// Agent-to-agent communication port. Agents never instantiate each other.
/// </summary>
public interface IAgentCommunication
{
    /// <summary>
    /// Sends a structured request to another agent resolved by <see cref="AgentRequest.AgentName"/>.
    /// </summary>
    Task<AgentResponse> SendAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

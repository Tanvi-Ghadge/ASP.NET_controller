using MyApi.AI.A2A;
using MyApi.AI.Harness;
using AgentResponse = MyApi.AI.A2A.AgentResponse;

namespace MyApi.AI.Agents;

/// <summary>
/// Contract for a specialized domain agent in the multi-agent platform.
/// </summary>
public interface IDomainAgent
{
    /// <summary>Stable routing key (e.g. employee-agent).</summary>
    string AgentKey { get; }

    /// <summary>Human-readable agent name.</summary>
    string AgentName { get; }

    /// <summary>Primary business domain this agent owns.</summary>
    string Domain { get; }

    /// <summary>
    /// Conversational / LLM execution using harness-prepared messages.
    /// </summary>
    Task<string> RunAsync(HarnessContext context);

    /// <summary>
    /// Structured agent-to-agent invocation.
    /// </summary>
    Task<AgentResponse> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default);
}

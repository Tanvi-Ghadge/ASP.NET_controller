using MyApi.AI.Memory;
using MyApi.AI.Sessions;

namespace MyApi.AI.Harness;

/// <summary>
/// Builds a <see cref="HarnessContext"/> from request inputs, session, and memory.
/// Prompt assembly happens later in the harness prepare-prompt stage.
/// </summary>
public interface IHarnessContextFactory
{
    /// <summary>
    /// Creates an execution context for the given user, session, message, and memories.
    /// </summary>
    HarnessContext Create(
        string userId,
        AgentSession session,
        string currentMessage,
        MemoryContext memory,
        string correlationId,
        CancellationToken cancellationToken);
}

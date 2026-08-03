using MyApi.AI.Harness;
using MyApi.AI.Models;

namespace MyApi.AI.Sessions;

/// <summary>
/// Legacy session-oriented facade. Prefer <see cref="IAgentHarness"/> for chat execution.
/// Kept for compatibility; delegates to the harness runtime.
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Continues or starts a conversation by delegating to the agent harness.
    /// </summary>
    Task<ChatResponse> ChatAsync(
        string? sessionId,
        string? userId,
        string message,
        CancellationToken cancellationToken = default);
}

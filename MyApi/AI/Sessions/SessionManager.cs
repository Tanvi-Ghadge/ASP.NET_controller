using MyApi.AI.Harness;
using MyApi.AI.Models;

namespace MyApi.AI.Sessions;

/// <summary>
/// Thin adapter over <see cref="IAgentHarness"/> for callers that still depend on session-manager naming.
/// The harness owns the real execution lifecycle.
/// </summary>
public sealed class SessionManager : ISessionManager
{
    private readonly IAgentHarness _harness;

    public SessionManager(IAgentHarness harness)
    {
        _harness = harness;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> ChatAsync(
        string? sessionId,
        string? userId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var result = await _harness.ExecuteAsync(sessionId, userId, message, cancellationToken);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(result.Error ?? "Harness execution failed.");
        }

        return new ChatResponse(result.SessionId, result.Response);
    }
}

using MyApi.AI.Memory;
using MyApi.AI.Sessions;

namespace MyApi.AI.Harness;

/// <summary>
/// Default <see cref="IHarnessContextFactory"/> implementation.
/// </summary>
public sealed class HarnessContextFactory : IHarnessContextFactory
{
    /// <inheritdoc />
    public HarnessContext Create(
        string userId,
        AgentSession session,
        string currentMessage,
        MemoryContext memory,
        string correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentMessage);
        ArgumentNullException.ThrowIfNull(memory);

        return new HarnessContext
        {
            UserId = userId,
            Session = session,
            ConversationHistory = session.Messages.ToList(),
            CurrentMessage = currentMessage,
            Memory = memory,
            CorrelationId = correlationId,
            Timestamp = DateTimeOffset.UtcNow,
            CancellationToken = cancellationToken
        };
    }
}

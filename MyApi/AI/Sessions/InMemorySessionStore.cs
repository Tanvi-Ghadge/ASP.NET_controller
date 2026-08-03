using System.Collections.Concurrent;

namespace MyApi.AI.Sessions;

/// <summary>
/// Thread-safe in-memory <see cref="ISessionStore"/> for development and Phase 1 multi-turn chat.
/// Not suitable for multi-instance production without sticky sessions or a distributed store.
/// </summary>
public sealed class InMemorySessionStore : ISessionStore
{
    private readonly ConcurrentDictionary<string, AgentSession> _sessions = new(StringComparer.Ordinal);
    private readonly ILogger<InMemorySessionStore> _logger;

    public InMemorySessionStore(ILogger<InMemorySessionStore> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<AgentSession> CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var session = new AgentSession();
        if (!_sessions.TryAdd(session.SessionId, Clone(session)))
        {
            throw new InvalidOperationException($"Failed to create session '{session.SessionId}'.");
        }

        _logger.LogInformation("Created chat session {SessionId}.", session.SessionId);
        return Task.FromResult(Clone(session));
    }

    /// <inheritdoc />
    public Task<AgentSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult<AgentSession?>(null);
        }

        return Task.FromResult(
            _sessions.TryGetValue(sessionId, out var session) ? Clone(session) : null);
    }

    /// <inheritdoc />
    public Task UpdateAsync(AgentSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(session.SessionId))
        {
            throw new ArgumentException("SessionId is required.", nameof(session));
        }

        session.Touch();
        _sessions[session.SessionId] = Clone(session);
        _logger.LogDebug(
            "Updated chat session {SessionId} with {MessageCount} messages.",
            session.SessionId,
            session.Messages.Count);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.FromResult(false);
        }

        var removed = _sessions.TryRemove(sessionId, out _);
        if (removed)
        {
            _logger.LogInformation("Deleted chat session {SessionId}.", sessionId);
        }

        return Task.FromResult(removed);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentSession>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AgentSession> list = _sessions.Values
            .Select(Clone)
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();

        return Task.FromResult(list);
    }

    /// <summary>
    /// Defensive copy so callers cannot mutate the store's internal instances.
    /// </summary>
    private static AgentSession Clone(AgentSession source)
    {
        return new AgentSession
        {
            SessionId = source.SessionId,
            CreatedAt = source.CreatedAt,
            LastActivityAt = source.LastActivityAt,
            Metadata = new SessionMetadata
            {
                Values = new Dictionary<string, string>(source.Metadata.Values, StringComparer.OrdinalIgnoreCase)
            },
            Messages = source.Messages.Select(m => new SessionMessage
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.Timestamp
            }).ToList()
        };
    }
}

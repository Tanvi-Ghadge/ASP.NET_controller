namespace MyApi.AI.Sessions;

/// <summary>
/// Persistence abstraction for application <see cref="AgentSession"/> instances.
/// Implementations may be in-memory, Redis, SQL, etc.
/// </summary>
public interface ISessionStore
{
    /// <summary>Creates and stores a new empty session.</summary>
    Task<AgentSession> CreateAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves a session by id, or <see langword="null"/> if missing.</summary>
    Task<AgentSession?> GetAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Replaces the stored session with the provided snapshot.</summary>
    Task UpdateAsync(AgentSession session, CancellationToken cancellationToken = default);

    /// <summary>Deletes a session. Returns <see langword="true"/> if it existed.</summary>
    Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>Lists all currently active (stored) sessions.</summary>
    Task<IReadOnlyList<AgentSession>> ListActiveAsync(CancellationToken cancellationToken = default);
}

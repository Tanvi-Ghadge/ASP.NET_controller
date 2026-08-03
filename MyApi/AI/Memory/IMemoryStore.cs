namespace MyApi.AI.Memory;

/// <summary>
/// Persistence port for long-term memories. Implementations may use SQL, Redis, Cosmos, or vector DBs.
/// </summary>
public interface IMemoryStore
{
    /// <summary>Inserts a new memory, or updates value/type when the same user+key already exists.</summary>
    Task<MemoryRecord> SaveMemoryAsync(MemoryRecord memory, CancellationToken cancellationToken = default);

    /// <summary>Updates an existing memory by id.</summary>
    Task<MemoryRecord?> UpdateMemoryAsync(MemoryRecord memory, CancellationToken cancellationToken = default);

    /// <summary>Deletes a memory by id.</summary>
    Task<bool> DeleteMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default);

    /// <summary>Gets a memory by id.</summary>
    Task<MemoryRecord?> GetMemoryAsync(Guid memoryId, CancellationToken cancellationToken = default);

    /// <summary>Gets a memory by user and key.</summary>
    Task<MemoryRecord?> GetMemoryByKeyAsync(string userId, string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches memories for a user. <paramref name="query"/> enables future semantic search;
    /// the SQL implementation currently applies simple key/value contains filtering.
    /// </summary>
    Task<IReadOnlyList<MemoryRecord>> SearchMemoryAsync(
        string userId,
        string? query = null,
        MemoryType? type = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all memories for a user.</summary>
    Task<IReadOnlyList<MemoryRecord>> GetUserMemoriesAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

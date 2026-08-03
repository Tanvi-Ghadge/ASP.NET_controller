namespace MyApi.AI.Memory;

/// <summary>
/// Retrieves relevant long-term memories before an agent turn.
/// Current strategy: user filter + lightweight relevance ranking.
/// Replace internals with vector search later without changing callers.
/// </summary>
public interface IMemoryRetrievalService
{
    /// <summary>
    /// Loads and ranks memories for <paramref name="userId"/> given the current user message.
    /// </summary>
    Task<MemoryContext> RetrieveAsync(
        string userId,
        string? currentMessage = null,
        CancellationToken cancellationToken = default);
}

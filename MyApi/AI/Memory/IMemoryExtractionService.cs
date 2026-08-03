namespace MyApi.AI.Memory;

/// <summary>
/// Detects durable facts/preferences in user utterances and persists them.
/// Conversation-only requests (e.g. "Show employee 5") must not create memories.
/// </summary>
public interface IMemoryExtractionService
{
    /// <summary>
    /// Analyzes the user message (and optional assistant reply) and returns candidate memories
    /// without persisting them.
    /// </summary>
    Task<IReadOnlyList<MemoryRecord>> ExtractAsync(
        string userId,
        string userMessage,
        string? assistantReply = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts candidates and saves them through <see cref="IMemoryStore"/>.
    /// </summary>
    Task<IReadOnlyList<MemoryRecord>> ExtractAndPersistAsync(
        string userId,
        string userMessage,
        string? assistantReply = null,
        CancellationToken cancellationToken = default);
}

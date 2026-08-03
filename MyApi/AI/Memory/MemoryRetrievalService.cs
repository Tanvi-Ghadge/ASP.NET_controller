namespace MyApi.AI.Memory;

/// <summary>
/// Default retrieval: filter by user, optionally by type, then rank by relevance to the current message.
/// Extensible toward embeddings / Azure AI Search / Pinecone behind the same interface.
/// </summary>
public sealed class MemoryRetrievalService : IMemoryRetrievalService
{
    private readonly IMemoryStore _memoryStore;
    private readonly ILogger<MemoryRetrievalService> _logger;

    public MemoryRetrievalService(
        IMemoryStore memoryStore,
        ILogger<MemoryRetrievalService> logger)
    {
        _memoryStore = memoryStore;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MemoryContext> RetrieveAsync(
        string userId,
        string? currentMessage = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return MemoryContext.Empty(userId ?? string.Empty);
        }

        var memories = await _memoryStore.GetUserMemoriesAsync(userId, cancellationToken);
        if (memories.Count == 0)
        {
            return MemoryContext.Empty(userId);
        }

        var ranked = Rank(memories, currentMessage)
            .Take(12)
            .ToList();

        _logger.LogInformation(
            "Retrieved {RelevantCount}/{TotalCount} memories for user {UserId}.",
            ranked.Count,
            memories.Count,
            userId);

        return new MemoryContext(userId, ranked);
    }

    /// <summary>
    /// Simple lexical ranking. Prefer profile/preference types; boost keyword overlap with the prompt.
    /// </summary>
    private static IEnumerable<MemoryRecord> Rank(
        IReadOnlyList<MemoryRecord> memories,
        string? currentMessage)
    {
        var terms = Tokenize(currentMessage);

        return memories
            .Select(m => new
            {
                Memory = m,
                Score = Score(m, terms)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Memory.UpdatedAt)
            .Select(x => x.Memory);
    }

    private static int Score(MemoryRecord memory, HashSet<string> terms)
    {
        var score = memory.Type switch
        {
            MemoryType.UserPreference => 50,
            MemoryType.UserProfile => 40,
            MemoryType.LearnedFact => 30,
            MemoryType.BusinessFact => 25,
            MemoryType.TemporaryPreference => 10,
            _ => 5
        };

        if (terms.Count == 0)
        {
            return score;
        }

        var haystack = $"{memory.Key} {memory.Value}".ToLowerInvariant();
        foreach (var term in terms)
        {
            if (haystack.Contains(term, StringComparison.Ordinal))
            {
                score += 15;
            }
        }

        // Report-related prompts should strongly surface report preferences.
        if (terms.Contains("report") || terms.Contains("payroll") || terms.Contains("generate"))
        {
            if (memory.Key.Contains("Report", StringComparison.OrdinalIgnoreCase) ||
                memory.Key.Contains("Format", StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
            }
        }

        return score;
    }

    private static HashSet<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return text
            .ToLowerInvariant()
            .Split([' ', '\t', '\r', '\n', ',', '.', '?', '!', ';', ':'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}

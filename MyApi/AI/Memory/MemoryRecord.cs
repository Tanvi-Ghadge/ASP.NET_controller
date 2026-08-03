namespace MyApi.AI.Memory;

/// <summary>
/// Domain model for a single long-term memory entry.
/// Independent from <c>AgentSession</c> conversation state.
/// </summary>
public sealed class MemoryRecord
{
    /// <summary>Primary key.</summary>
    public Guid MemoryId { get; set; }

    /// <summary>Owner of the memory (stable across sessions).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Stable key such as PreferredReportFormat.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Stored value such as PDF.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Memory category.</summary>
    public MemoryType Type { get; set; }

    /// <summary>When the memory was first created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the memory was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

using System.Text;

namespace MyApi.AI.Memory;

/// <summary>
/// Execution context that carries retrieved long-term memories into an agent turn.
/// Designed so a future vector store can populate the same shape without changing the agent.
/// </summary>
public sealed class MemoryContext
{
    /// <summary>User whose memories were loaded.</summary>
    public string UserId { get; }

    /// <summary>Relevant memories for this turn (already filtered/ranked).</summary>
    public IReadOnlyList<MemoryRecord> Memories { get; }

    public MemoryContext(string userId, IReadOnlyList<MemoryRecord> memories)
    {
        UserId = userId;
        Memories = memories;
    }

    /// <summary>Empty context used when the user has no memories.</summary>
    public static MemoryContext Empty(string userId) =>
        new(userId, Array.Empty<MemoryRecord>());

    /// <summary>True when at least one memory is present.</summary>
    public bool HasMemories => Memories.Count > 0;

    /// <summary>
    /// Builds a compact system-prompt fragment for the LLM.
    /// Does not dump unrelated data — only the retrieved set.
    /// </summary>
    public string ToSystemPromptFragment()
    {
        if (!HasMemories)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine("Long-term user memory (persist across conversations — honor these preferences):");

        foreach (var memory in Memories)
        {
            sb.Append("- [")
                .Append(memory.Type)
                .Append("] ")
                .Append(memory.Key)
                .Append(" = ")
                .Append(memory.Value)
                .AppendLine();
        }

        sb.AppendLine("When the user asks to generate a report or similar output, apply matching preferences automatically and mention them briefly.");
        return sb.ToString().TrimEnd();
    }
}

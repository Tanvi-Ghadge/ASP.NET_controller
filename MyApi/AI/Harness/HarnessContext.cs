using Microsoft.Extensions.AI;
using MyApi.AI.Memory;
using MyApi.AI.Sessions;

namespace MyApi.AI.Harness;

/// <summary>
/// Fully prepared execution bag for one harness run.
/// The agent consumes this context and must not load sessions or memory itself.
/// </summary>
public sealed class HarnessContext
{
    /// <summary>Stable user identity for memory scoping.</summary>
    public required string UserId { get; init; }

    /// <summary>Application conversation session (mutable; updated after the agent runs).</summary>
    public required AgentSession Session { get; init; }

    /// <summary>Prior session messages (before the current user turn).</summary>
    public required IReadOnlyList<SessionMessage> ConversationHistory { get; init; }

    /// <summary>Current user utterance.</summary>
    public required string CurrentMessage { get; init; }

    /// <summary>Retrieved long-term memories for this turn.</summary>
    public required MemoryContext Memory { get; init; }

    /// <summary>
    /// Prompt messages assembled by the harness (system memory + history + user message).
    /// The agent should invoke the LLM with this list as-is.
    /// </summary>
    public IReadOnlyList<ChatMessage> PreparedMessages { get; set; } = Array.Empty<ChatMessage>();

    /// <summary>Correlation id for distributed tracing / log correlation.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>UTC timestamp when the harness run started.</summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>Cancellation token for the run.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Extensible metadata for future workflow ids, approval tokens, checkpoints, etc.
    /// </summary>
    public Dictionary<string, object?> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
}

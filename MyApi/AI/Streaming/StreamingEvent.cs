using System.Text.Json.Serialization;
using MyApi.AI.AGUI;

namespace MyApi.AI.Streaming;

/// <summary>
/// Transport envelope wrapping a strongly typed AG-UI event for SignalR / SSE.
/// </summary>
public sealed class StreamingEvent
{
    /// <summary>Envelope id.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>Correlation id for the execution.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Session that owns this event (may be empty until session is loaded).</summary>
    public string? SessionId { get; init; }

    /// <summary>Coarse transport type.</summary>
    public required StreamingEventType Type { get; init; }

    /// <summary>UTC timestamp.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Strongly typed AG-UI payload.</summary>
    public required AgentUiEvent Payload { get; init; }

    /// <summary>Sequence number within the execution (monotonic).</summary>
    public long Sequence { get; init; }
}

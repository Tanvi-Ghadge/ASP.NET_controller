using MyApi.AI.Memory;

namespace MyApi.AI.A2A;

/// <summary>
/// Structured request from one agent (or workflow step) to another.
/// </summary>
public sealed class AgentRequest
{
    /// <summary>Target agent key (routing id).</summary>
    public required string AgentName { get; init; }

    /// <summary>Operation / capability name on the target agent.</summary>
    public required string Operation { get; init; }

    /// <summary>Correlation id for distributed tracing.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Opaque request payload (DTOs, primitives, dictionaries).</summary>
    public object? RequestData { get; init; }

    /// <summary>Optional session id for conversational continuity.</summary>
    public string? SessionId { get; init; }

    /// <summary>Optional long-term memory snapshot.</summary>
    public MemoryContext? MemoryContext { get; init; }

    /// <summary>Caller / workflow metadata.</summary>
    public Dictionary<string, object?> ExecutionMetadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>UTC timestamp when the request was created.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

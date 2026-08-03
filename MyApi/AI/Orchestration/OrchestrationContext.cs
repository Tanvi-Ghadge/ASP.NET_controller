using MyApi.AI.Harness;

namespace MyApi.AI.Orchestration;

/// <summary>
/// Input bag for orchestration decision-making.
/// Built by the harness; never contains SQL or plugin execution APIs.
/// </summary>
public sealed class OrchestrationContext
{
    /// <summary>Prepared harness context (session, memory, prompt).</summary>
    public required HarnessContext Harness { get; init; }

    /// <summary>Raw user utterance.</summary>
    public required string UserMessage { get; init; }

    /// <summary>Stable user id.</summary>
    public required string UserId { get; init; }

    /// <summary>Correlation id for telemetry.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>Cancellation token for the run.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Extensible metadata for planners, approvals, and multi-agent hops.</summary>
    public Dictionary<string, object?> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
}

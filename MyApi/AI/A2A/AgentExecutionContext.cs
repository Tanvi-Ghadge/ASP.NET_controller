using MyApi.AI.Harness;
using MyApi.AI.Memory;
using MyApi.AI.Sessions;

namespace MyApi.AI.A2A;

/// <summary>
/// Ambient execution context available during A2A handling.
/// </summary>
public sealed class AgentExecutionContext
{
    public required string CorrelationId { get; init; }

    public string? SessionId { get; init; }

    public MemoryContext? Memory { get; init; }

    public HarnessContext? Harness { get; init; }

    public AgentSession? Session { get; init; }

    public CancellationToken CancellationToken { get; init; }

    public Dictionary<string, object?> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static AgentExecutionContext FromRequest(AgentRequest request, CancellationToken cancellationToken) =>
        new()
        {
            CorrelationId = request.CorrelationId,
            SessionId = request.SessionId,
            Memory = request.MemoryContext,
            CancellationToken = cancellationToken,
            Metadata = new Dictionary<string, object?>(request.ExecutionMetadata, StringComparer.OrdinalIgnoreCase)
        };
}

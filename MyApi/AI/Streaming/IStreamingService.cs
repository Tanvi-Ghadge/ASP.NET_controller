using System.Threading.Channels;
using MyApi.AI.AGUI;

namespace MyApi.AI.Streaming;

/// <summary>
/// Publishes AG-UI events to transports (SignalR, SSE channels) without coupling domain code to SignalR.
/// </summary>
public interface IStreamingService
{
    /// <summary>Sets the ambient streaming context for the current async flow.</summary>
    IDisposable BeginScope(StreamingContext context);

    /// <summary>Current ambient context, if any.</summary>
    StreamingContext? Current { get; }

    /// <summary>Publishes a strongly typed AG-UI event.</summary>
    Task PublishAsync(AgentUiEvent payload, CancellationToken cancellationToken = default);

    /// <summary>Publishes using an explicit context (when ambient is unavailable).</summary>
    Task PublishAsync(
        StreamingContext context,
        AgentUiEvent payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams an already-complete string as incremental tokens (word/chunk based).
    /// Used when the LLM path did not stream natively.
    /// </summary>
    Task StreamTextAsTokensAsync(
        StreamingContext context,
        string fullText,
        string? agentName = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates an in-process subscription for SSE (keyed by correlation id).</summary>
    IAsyncEnumerable<StreamingEvent> SubscribeAsync(
        string correlationId,
        CancellationToken cancellationToken = default);

    /// <summary>Registers a channel for the correlation id before execution starts.</summary>
    IDisposable RegisterExecution(string correlationId);
}

/// <summary>
/// Event publisher abstraction — same semantic as streaming publish, kept for DI clarity.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(
        StreamingContext context,
        AgentUiEvent payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outbound transport for streaming events (SignalR is one implementation).
/// </summary>
public interface IStreamingTransport
{
    Task SendAsync(StreamingEvent streamingEvent, CancellationToken cancellationToken = default);
}

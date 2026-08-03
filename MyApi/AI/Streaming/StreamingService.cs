using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using MyApi.AI.AGUI;

namespace MyApi.AI.Streaming;

/// <summary>
/// Default streaming facade: Serilog + SignalR transport + in-memory SSE channels.
/// Domain layers depend only on this interface — never on <see cref="Hub"/>.
/// </summary>
public sealed class StreamingService : IStreamingService, IEventPublisher
{
    private static readonly AsyncLocal<StreamingContext?> Ambient = new();

    private readonly ConcurrentDictionary<string, ExecutionChannel> _channels = new(StringComparer.Ordinal);
    private readonly IEnumerable<IStreamingTransport> _transports;
    private readonly ILogger<StreamingService> _logger;
    private long _sequence;

    public StreamingService(
        IEnumerable<IStreamingTransport> transports,
        ILogger<StreamingService> logger)
    {
        _transports = transports;
        _logger = logger;
    }

    /// <inheritdoc />
    public StreamingContext? Current => Ambient.Value;

    /// <inheritdoc />
    public IDisposable BeginScope(StreamingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var previous = Ambient.Value;
        Ambient.Value = context;
        return new Scope(() => Ambient.Value = previous);
    }

    /// <inheritdoc />
    public IDisposable RegisterExecution(string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var channel = Channel.CreateUnbounded<StreamingEvent>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false
        });
        var execution = new ExecutionChannel(channel);

        // Idempotent: SSE may register before the harness; the first caller owns completion.
        if (!_channels.TryAdd(correlationId, execution))
        {
            return new Scope(() => { });
        }

        return new Scope(() =>
        {
            if (_channels.TryRemove(correlationId, out var removed))
            {
                removed.Writer.TryComplete();
            }
        });
    }

    /// <inheritdoc />
    public async Task PublishAsync(AgentUiEvent payload, CancellationToken cancellationToken = default)
    {
        var context = Ambient.Value
            ?? throw new InvalidOperationException(
                "No ambient StreamingContext. Call BeginScope or PublishAsync(context, ...).");
        await PublishAsync(context, payload, cancellationToken);
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        StreamingContext context,
        AgentUiEvent payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(payload);

        var type = MapType(payload);
        var seq = Interlocked.Increment(ref _sequence);
        var envelope = new StreamingEvent
        {
            CorrelationId = context.CorrelationId,
            SessionId = context.SessionId,
            Type = type,
            Payload = payload,
            Sequence = seq,
            Timestamp = DateTimeOffset.UtcNow
        };

        _logger.LogInformation(
            "AG-UI Event. Type={EventType}, CorrelationId={CorrelationId}, SessionId={SessionId}, Sequence={Sequence}, WorkflowId={WorkflowId}",
            type,
            context.CorrelationId,
            context.SessionId,
            seq,
            context.WorkflowId);

        if (_channels.TryGetValue(context.CorrelationId, out var execution))
        {
            await execution.Writer.WriteAsync(envelope, cancellationToken);
        }

        foreach (var transport in _transports)
        {
            try
            {
                await transport.SendAsync(envelope, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Streaming transport {Transport} failed. CorrelationId={CorrelationId}",
                    transport.GetType().Name,
                    context.CorrelationId);
            }
        }
    }

    /// <inheritdoc />
    public async Task StreamTextAsTokensAsync(
        StreamingContext context,
        string fullText,
        string? agentName = null,
        CancellationToken cancellationToken = default)
    {
        fullText ??= string.Empty;

        await PublishAsync(
            context,
            new StreamingResponseStartedEvent
            {
                CorrelationId = context.CorrelationId,
                AgentName = agentName
            },
            cancellationToken);

        var accumulated = string.Empty;
        var index = 0;
        foreach (var token in Tokenize(fullText))
        {
            cancellationToken.ThrowIfCancellationRequested();
            accumulated += token;
            await PublishAsync(
                context,
                new TokenStreamEvent
                {
                    CorrelationId = context.CorrelationId,
                    Token = token,
                    AccumulatedText = accumulated,
                    TokenIndex = index++
                },
                cancellationToken);

            // Small delay so SSE/Swagger clients can observe incremental updates.
            await Task.Delay(12, cancellationToken);
        }

        await PublishAsync(
            context,
            new StreamingCompletedEvent
            {
                CorrelationId = context.CorrelationId,
                FullText = fullText,
                TokenCount = index
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<StreamingEvent> SubscribeAsync(
        string correlationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(correlationId, out var execution))
        {
            yield break;
        }

        await foreach (var item in execution.Reader.ReadAllAsync(cancellationToken))
        {
            yield return item;
        }
    }

    private static StreamingEventType MapType(AgentUiEvent payload) => payload switch
    {
        ExecutionStartedEvent => StreamingEventType.ExecutionStarted,
        ExecutionCompletedEvent => StreamingEventType.ExecutionFinished,
        ErrorEvent => StreamingEventType.Error,
        SessionLoadedEvent => StreamingEventType.SessionLoaded,
        MemoryLoadedEvent => StreamingEventType.MemoryRetrieved,
        PlanningEvent p when p.Phase.Equals("started", StringComparison.OrdinalIgnoreCase)
            => StreamingEventType.PlanningStarted,
        PlanningEvent => StreamingEventType.PlanningCompleted,
        WorkflowStartedEvent => StreamingEventType.WorkflowStarted,
        WorkflowCompletedEvent => StreamingEventType.WorkflowCompleted,
        WorkflowStepStartedEvent => StreamingEventType.StepStarted,
        WorkflowStepCompletedEvent => StreamingEventType.StepCompleted,
        WorkflowStepFailedEvent => StreamingEventType.StepFailed,
        WorkflowStepSkippedEvent => StreamingEventType.StepSkipped,
        AgentSelectedEvent => StreamingEventType.AgentSelected,
        AgentStartedEvent => StreamingEventType.AgentStarted,
        AgentCompletedEvent => StreamingEventType.AgentCompleted,
        ToolStartedEvent => StreamingEventType.ToolStarted,
        ToolCompletedEvent => StreamingEventType.ToolCompleted,
        PluginInvokedEvent => StreamingEventType.PluginInvoked,
        DelegationStartedEvent => StreamingEventType.DelegationStarted,
        DelegationFinishedEvent => StreamingEventType.DelegationFinished,
        StreamingResponseStartedEvent => StreamingEventType.StreamingResponseStarted,
        TokenStreamEvent => StreamingEventType.StreamingToken,
        StreamingCompletedEvent => StreamingEventType.StreamingCompleted,
        WorkflowPausedEvent => StreamingEventType.WorkflowPaused,
        WorkflowResumedEvent => StreamingEventType.WorkflowResumed,
        ApprovalRequestedEvent => StreamingEventType.ApprovalRequested,
        ApprovalGrantedEvent => StreamingEventType.ApprovalGranted,
        ApprovalRejectedEvent => StreamingEventType.ApprovalRejected,
        CheckpointSavedEvent => StreamingEventType.CheckpointSaved,
        CheckpointLoadedEvent => StreamingEventType.CheckpointLoaded,
        TelemetrySnapshotEvent => StreamingEventType.TelemetrySnapshot,
        _ => StreamingEventType.Error
    };

    private static IEnumerable<string> Tokenize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            yield return i == 0 ? parts[i] : " " + parts[i];
        }
    }

    private sealed class ExecutionChannel
    {
        private readonly Channel<StreamingEvent> _channel;

        public ExecutionChannel(Channel<StreamingEvent> channel) => _channel = channel;

        public ChannelWriter<StreamingEvent> Writer => _channel.Writer;

        public ChannelReader<StreamingEvent> Reader => _channel.Reader;
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;
        private int _disposed;

        public Scope(Action onDispose) => _onDispose = onDispose;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _onDispose();
            }
        }
    }
}

/// <summary>
/// SignalR transport — pushes envelopes to session and execution groups.
/// </summary>
public sealed class SignalRStreamingTransport : IStreamingTransport
{
    public const string ClientMethod = "agentEvent";

    private readonly IHubContext<AgentHub> _hub;
    private readonly ILogger<SignalRStreamingTransport> _logger;

    public SignalRStreamingTransport(IHubContext<AgentHub> hub, ILogger<SignalRStreamingTransport> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(StreamingEvent streamingEvent, CancellationToken cancellationToken = default)
    {
        var payload = streamingEvent;

        if (!string.IsNullOrWhiteSpace(streamingEvent.SessionId))
        {
            await _hub.Clients
                .Group(AgentHub.SessionGroup(streamingEvent.SessionId))
                .SendAsync(ClientMethod, payload, cancellationToken);
        }

        await _hub.Clients
            .Group(AgentHub.ExecutionGroup(streamingEvent.CorrelationId))
            .SendAsync(ClientMethod, payload, cancellationToken);

        _logger.LogDebug(
            "SignalR event dispatched. Type={Type}, CorrelationId={CorrelationId}, SessionId={SessionId}",
            streamingEvent.Type,
            streamingEvent.CorrelationId,
            streamingEvent.SessionId);
    }
}

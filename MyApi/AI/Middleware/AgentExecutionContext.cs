using System.Diagnostics;
using MyApi.AI.Harness;
using MyApi.AI.Memory;
using MyApi.AI.Observability;
using MyApi.AI.Orchestration;
using MyApi.AI.Streaming;

namespace MyApi.AI.Middleware;

/// <summary>
/// Mutable execution bag shared across the AI middleware pipeline.
/// Distinct from ASP.NET <c>HttpContext</c> and from A2A ambient context.
/// </summary>
public sealed class AgentExecutionContext
{
    private static readonly AsyncLocal<AgentExecutionContext?> Ambient = new();

    /// <summary>Ambient context for tool middleware during the current async flow.</summary>
    public static AgentExecutionContext? Current
    {
        get => Ambient.Value;
        private set => Ambient.Value = value;
    }

    public required string ExecutionId { get; init; }

    public required string CorrelationId { get; init; }

    public required string UserId { get; set; }

    public string? SessionId { get; set; }

    public required string UserMessage { get; set; }

    public string? MaskedUserMessage { get; set; }

    public HarnessContext? Harness { get; set; }

    public OrchestrationContext? Orchestration { get; set; }

    public OrchestrationResult? OrchestrationResult { get; set; }

    public string? FinalResponse { get; set; }

    public string? FilteredResponse { get; set; }

    public bool IsAuthenticated { get; set; }

    public bool IsAuthorized { get; set; } = true;

    public string? RejectedReason { get; set; }

    public string? CurrentToolName { get; set; }

    public string? CurrentPluginName { get; set; }

    public object? CurrentToolParameters { get; set; }

    public object? CurrentToolResult { get; set; }

    public bool CurrentToolSucceeded { get; set; } = true;

    public string? CurrentToolError { get; set; }

    public int ToolRetryCount { get; set; }

    public StreamingContext? Streaming { get; set; }

    public CancellationToken CancellationToken { get; set; }

    public ExecutionTelemetry Telemetry { get; } = new();

    public Dictionary<string, object?> Items { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Activity? RootActivity { get; set; }

    /// <summary>Sets this instance as ambient for the current async flow.</summary>
    public IDisposable BeginAmbientScope()
    {
        var previous = Ambient.Value;
        Ambient.Value = this;
        return new AmbientScope(() => Ambient.Value = previous);
    }

    private sealed class AmbientScope : IDisposable
    {
        private readonly Action _restore;
        private int _disposed;

        public AmbientScope(Action restore) => _restore = restore;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _restore();
            }
        }
    }
}

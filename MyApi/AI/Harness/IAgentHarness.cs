using MyApi.AI.Streaming;

namespace MyApi.AI.Harness;

/// <summary>
/// Primary execution runtime for AI requests.
/// Owns session/memory lifecycle, prompt preparation, telemetry, AG-UI events, and agent invocation.
/// </summary>
public interface IAgentHarness
{
    /// <summary>
    /// Executes a full chat turn through the harness pipeline.
    /// </summary>
    Task<HarnessExecutionResult> ExecuteAsync(
        string? sessionId,
        string? userId,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a chat turn with an explicit streaming context (SSE / SignalR correlation).
    /// </summary>
    Task<HarnessExecutionResult> ExecuteAsync(
        string? sessionId,
        string? userId,
        string message,
        StreamingContext? streamingContext,
        CancellationToken cancellationToken = default);
}

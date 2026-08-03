using System.Text;
using Microsoft.Agents.AI;
using MyApi.AI.AGUI;
using MyApi.AI.Harness;
using MyApi.AI.Streaming;

namespace MyApi.AI.Agents;

/// <summary>
/// Shared helper: runs an <see cref="AIAgent"/> with AG-UI token streaming when a streaming context exists.
/// </summary>
public static class AgentStreamingRunner
{
    /// <summary>
    /// Streams LLM updates when possible; otherwise falls back to <see cref="AIAgent.RunAsync"/> and
    /// synthesizes token events from the final text.
    /// </summary>
    public static async Task<string> RunWithStreamingAsync(
        AIAgent agent,
        HarnessContext harness,
        IStreamingService streaming,
        string agentKey,
        string agentName,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var context = streaming.Current ?? new StreamingContext
        {
            CorrelationId = harness.CorrelationId,
            SessionId = harness.Session.SessionId,
            UserId = harness.UserId
        };

        await streaming.PublishAsync(
            context,
            new AgentStartedEvent
            {
                CorrelationId = context.CorrelationId,
                AgentKey = agentKey,
                AgentName = agentName,
                Mode = "conversational"
            },
            cancellationToken);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var accumulated = new StringBuilder();
        var tokenIndex = 0;

        await streaming.PublishAsync(
            context,
            new StreamingResponseStartedEvent
            {
                CorrelationId = context.CorrelationId,
                AgentName = agentName
            },
            cancellationToken);

        try
        {
            await foreach (var update in agent.RunStreamingAsync(
                               harness.PreparedMessages,
                               session: null,
                               cancellationToken: cancellationToken))
            {
                var delta = update.Text;
                if (string.IsNullOrEmpty(delta))
                {
                    continue;
                }

                accumulated.Append(delta);
                await streaming.PublishAsync(
                    context,
                    new TokenStreamEvent
                    {
                        CorrelationId = context.CorrelationId,
                        Token = delta,
                        AccumulatedText = accumulated.ToString(),
                        TokenIndex = tokenIndex++
                    },
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "RunStreamingAsync unavailable or failed for {AgentName}; falling back to RunAsync.",
                agentName);

            var response = await agent.RunAsync(
                harness.PreparedMessages,
                session: null,
                cancellationToken: cancellationToken);
            var text = response.Text ?? string.Empty;
            accumulated.Clear();
            accumulated.Append(text);
            await streaming.StreamTextAsTokensAsync(context, text, agentName, cancellationToken);
            sw.Stop();

            await streaming.PublishAsync(
                context,
                new AgentCompletedEvent
                {
                    CorrelationId = context.CorrelationId,
                    AgentKey = agentKey,
                    AgentName = agentName,
                    DurationMs = sw.ElapsedMilliseconds,
                    Succeeded = true
                },
                cancellationToken);

            harness.Metadata["TokensStreamed"] = true;
            return text;
        }

        var full = accumulated.ToString();
        await streaming.PublishAsync(
            context,
            new StreamingCompletedEvent
            {
                CorrelationId = context.CorrelationId,
                FullText = full,
                TokenCount = tokenIndex
            },
            cancellationToken);

        sw.Stop();
        await streaming.PublishAsync(
            context,
            new AgentCompletedEvent
            {
                CorrelationId = context.CorrelationId,
                AgentKey = agentKey,
                AgentName = agentName,
                DurationMs = sw.ElapsedMilliseconds,
                Succeeded = true
            },
            cancellationToken);

        harness.Metadata["TokensStreamed"] = true;
        return full;
    }
}

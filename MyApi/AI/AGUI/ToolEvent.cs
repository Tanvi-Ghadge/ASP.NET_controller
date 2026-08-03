namespace MyApi.AI.AGUI;

/// <summary>
/// Tool-centric view of AG-UI tool lifecycle events.
/// </summary>
public static class ToolEvent
{
    public static ToolStartedEvent Started(string correlationId, string toolName, string? agentName = null) =>
        new()
        {
            CorrelationId = correlationId,
            ToolName = toolName,
            AgentName = agentName
        };

    public static ToolCompletedEvent Completed(
        string correlationId,
        string toolName,
        long durationMs,
        bool succeeded = true,
        string? agentName = null) =>
        new()
        {
            CorrelationId = correlationId,
            ToolName = toolName,
            AgentName = agentName,
            DurationMs = durationMs,
            Succeeded = succeeded
        };
}

namespace MyApi.AI.A2A;

/// <summary>
/// Structured response from an agent handling an A2A request.
/// </summary>
public sealed class AgentResponse
{
    public required string AgentName { get; init; }

    public required AgentResponseStatus Status { get; init; }

    public object? Payload { get; init; }

    public string? TextOutput { get; init; }

    public TimeSpan ExecutionTime { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public Dictionary<string, object?> Metadata { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool Succeeded => Status == AgentResponseStatus.Success;

    public static AgentResponse Success(string agentName, object? payload, string? text, TimeSpan elapsed) =>
        new()
        {
            AgentName = agentName,
            Status = AgentResponseStatus.Success,
            Payload = payload,
            TextOutput = text,
            ExecutionTime = elapsed
        };

    public static AgentResponse Failure(string agentName, string error, TimeSpan elapsed) =>
        new()
        {
            AgentName = agentName,
            Status = AgentResponseStatus.Failure,
            Errors = [error],
            ExecutionTime = elapsed
        };
}

/// <summary>
/// Outcome of an A2A call.
/// </summary>
public enum AgentResponseStatus
{
    Success = 0,
    Failure = 1,
    Rejected = 2
}

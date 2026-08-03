namespace MyApi.AI.AGUI;

/// <summary>
/// Lightweight progress snapshot for UI progress bars / steppers.
/// </summary>
public sealed class AgentProgress
{
    public required string CorrelationId { get; init; }

    public string? SessionId { get; init; }

    public string? CurrentStage { get; init; }

    public string? CurrentAgent { get; init; }

    public string? CurrentWorkflow { get; init; }

    public string? CurrentStep { get; init; }

    public int CompletedSteps { get; init; }

    public int TotalSteps { get; init; }

    public double PercentComplete =>
        TotalSteps <= 0 ? 0 : Math.Clamp(100.0 * CompletedSteps / TotalSteps, 0, 100);

    public bool IsStreaming { get; init; }

    public string? PartialResponse { get; init; }
}

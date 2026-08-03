namespace MyApi.AI.Observability;

/// <summary>
/// Aggregated telemetry for one AI execution (agent + tools + workflow + LLM estimates).
/// </summary>
public sealed class ExecutionTelemetry
{
    public string? Model { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens => PromptTokens + CompletionTokens;

    public decimal EstimatedCostUsd { get; set; }

    public long TotalDurationMs { get; set; }

    public long AgentDurationMs { get; set; }

    public int PromptLength { get; set; }

    public int ResponseLength { get; set; }

    public double? Temperature { get; set; }

    public double? TopP { get; set; }

    public string? FinishReason { get; set; }

    public int MemoryRetrieved { get; set; }

    public bool SessionLoaded { get; set; }

    public string? WorkflowId { get; set; }

    public string? WorkflowName { get; set; }

    public string? AgentName { get; set; }

    public string? AgentKey { get; set; }

    public List<ToolInvocationTelemetry> Tools { get; } = [];

    public List<string> Timeline { get; } = [];

    public void Mark(string eventName) =>
        Timeline.Add($"{DateTimeOffset.UtcNow:O}|{eventName}");
}

/// <summary>
/// Per-tool invocation metrics.
/// </summary>
public sealed class ToolInvocationTelemetry
{
    public required string ToolName { get; init; }

    public string? PluginName { get; init; }

    public long DurationMs { get; init; }

    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public int RetryCount { get; init; }

    public string? ParametersPreview { get; init; }

    public string? ResultPreview { get; init; }
}

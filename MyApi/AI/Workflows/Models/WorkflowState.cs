namespace MyApi.AI.Workflows.Models;

/// <summary>
/// Mutable runtime state for one workflow execution.
/// Extensible for checkpoints, branch tokens, and compensation bookmarks.
/// </summary>
public sealed class WorkflowState
{
    /// <summary>Workflow instance id (one run). Restored from checkpoint on resume.</summary>
    public Guid InstanceId { get; set; } = Guid.NewGuid();

    /// <summary>Definition id being executed.</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>Current status.</summary>
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Pending;

    /// <summary>Zero-based index of the active step.</summary>
    public int CurrentStepIndex { get; set; }

    /// <summary>Name of the active step.</summary>
    public string? CurrentStepName { get; set; }

    /// <summary>Shared bag of step outputs / intermediate values.</summary>
    public Dictionary<string, object?> Variables { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>UTC created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>UTC started.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>UTC completed.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Last error message, if any.</summary>
    public string? LastError { get; set; }

    /// <summary>Gets a typed variable or default.</summary>
    public T? Get<T>(string key)
    {
        if (!Variables.TryGetValue(key, out var value) || value is null)
        {
            return default;
        }

        return value is T typed ? typed : default;
    }

    /// <summary>Sets a variable.</summary>
    public void Set(string key, object? value) => Variables[key] = value;
}

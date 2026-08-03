using MyApi.AI.Harness;
using MyApi.AI.Memory;
using MyApi.AI.Sessions;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Engine;

/// <summary>
/// Shared execution bag passed to every workflow step.
/// Extensible for parallel branches, approval tokens, and nested workflow stacks.
/// </summary>
public sealed class WorkflowExecutionContext
{
    /// <summary>Parent harness context (session, memory, prepared messages).</summary>
    public required HarnessContext Harness { get; init; }

    /// <summary>Mutable workflow instance state.</summary>
    public required WorkflowState State { get; init; }

    /// <summary>Convenience: current user id.</summary>
    public string UserId => Harness.UserId;

    /// <summary>Convenience: agent session.</summary>
    public AgentSession Session => Harness.Session;

    /// <summary>Convenience: long-term memory.</summary>
    public MemoryContext Memory => Harness.Memory;

    /// <summary>Convenience: correlation id.</summary>
    public string CorrelationId => Harness.CorrelationId;

    /// <summary>Cancellation token for the run.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Execution metadata (workflow name, attempt, future checkpoint id, etc.).</summary>
    public Dictionary<string, object?> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Workflow-scoped variables (alias of <see cref="WorkflowState.Variables"/>).</summary>
    public Dictionary<string, object?> Variables => State.Variables;
}

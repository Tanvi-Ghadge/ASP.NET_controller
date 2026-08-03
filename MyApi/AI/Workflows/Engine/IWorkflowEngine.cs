using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Engine;

/// <summary>
/// Entry point used by the harness. Loads a workflow definition and executes it.
/// Supports pause / resume / cancel / suspend for HITL.
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>Executes a workflow by definition id.</summary>
    Task<WorkflowExecutionResult> ExecuteAsync(
        string workflowId,
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>Resumes a paused instance from its durable checkpoint.</summary>
    Task<WorkflowExecutionResult> ResumeAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    /// <summary>Cancels a paused / waiting workflow instance.</summary>
    Task CancelAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>Suspends a waiting workflow (soft pause for later resume).</summary>
    Task SuspendAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>Lists registered workflow definition ids.</summary>
    IReadOnlyCollection<string> RegisteredWorkflowIds { get; }
}

namespace MyApi.AI.Orchestration;

/// <summary>
/// Brain of the application: decides what to run, then delegates execution to the workflow engine.
/// Must not execute SQL, plugins, or services directly.
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// Builds an execution plan and runs it through the workflow engine.
    /// </summary>
    Task<OrchestrationResult> OrchestrateAsync(
        OrchestrationContext context,
        CancellationToken cancellationToken = default);
}

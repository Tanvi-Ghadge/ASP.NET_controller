namespace MyApi.AI.Orchestration;

/// <summary>
/// How the orchestrator should run selected work.
/// Only <see cref="Sequential"/> is executed today; others are reserved for multi-agent/parallel plans.
/// </summary>
public enum ExecutionStrategy
{
    /// <summary>Run one workflow (or ordered list) from start to finish.</summary>
    Sequential = 0,

    /// <summary>Future: run independent workflows/agents concurrently.</summary>
    Parallel = 1,

    /// <summary>Future: branch based on intermediate outcomes.</summary>
    Conditional = 2
}

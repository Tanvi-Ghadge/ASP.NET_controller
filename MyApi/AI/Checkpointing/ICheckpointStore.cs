namespace MyApi.AI.Checkpointing;

/// <summary>
/// Durable store for workflow checkpoints (SQL-backed in production).
/// </summary>
public interface ICheckpointStore
{
    Task SaveAsync(WorkflowCheckpoint checkpoint, CancellationToken cancellationToken = default);

    Task<WorkflowCheckpoint?> GetByInstanceIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);

    Task<WorkflowCheckpoint?> GetByCheckpointIdAsync(
        Guid checkpointId,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        Guid workflowInstanceId,
        string status,
        CancellationToken cancellationToken = default);
}

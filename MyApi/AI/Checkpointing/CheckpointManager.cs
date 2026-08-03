using System.Text.Json;
using MyApi.AI.AGUI;
using MyApi.AI.Streaming;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Checkpointing;

/// <summary>
/// Creates and loads durable checkpoints around HITL pauses.
/// </summary>
public interface ICheckpointManager
{
    /// <summary>Persists a checkpoint when the workflow pauses for approval.</summary>
    Task<WorkflowCheckpoint> SavePauseCheckpointAsync(
        WorkflowExecutionContext context,
        IWorkflowDefinition definition,
        int resumeStepIndex,
        IReadOnlyList<string> completedSteps,
        Guid? approvalRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>Loads a checkpoint by workflow instance id.</summary>
    Task<WorkflowCheckpoint?> LoadAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>Marks a checkpoint as completed / cancelled / etc.</summary>
    Task MarkStatusAsync(
        Guid workflowInstanceId,
        WorkflowStatus status,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default checkpoint manager — publishes AG-UI events and delegates persistence to <see cref="ICheckpointStore"/>.
/// </summary>
public sealed class CheckpointManager : ICheckpointManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICheckpointStore _store;
    private readonly IStreamingService _streaming;
    private readonly ILogger<CheckpointManager> _logger;

    public CheckpointManager(
        ICheckpointStore store,
        IStreamingService streaming,
        ILogger<CheckpointManager> logger)
    {
        _store = store;
        _streaming = streaming;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkflowCheckpoint> SavePauseCheckpointAsync(
        WorkflowExecutionContext context,
        IWorkflowDefinition definition,
        int resumeStepIndex,
        IReadOnlyList<string> completedSteps,
        Guid? approvalRequestId,
        CancellationToken cancellationToken = default)
    {
        var streamCtx = _streaming.Current ?? new StreamingContext
        {
            CorrelationId = context.CorrelationId,
            SessionId = context.Session.SessionId,
            UserId = context.UserId,
            WorkflowId = definition.WorkflowId
        };

        var checkpoint = new WorkflowCheckpoint
        {
            CheckpointId = Guid.NewGuid(),
            WorkflowInstanceId = context.State.InstanceId,
            WorkflowDefinitionId = definition.WorkflowId,
            WorkflowName = definition.WorkflowName,
            ResumeStepIndex = resumeStepIndex,
            CompletedSteps = completedSteps.ToList(),
            Variables = new Dictionary<string, object?>(context.State.Variables, StringComparer.OrdinalIgnoreCase),
            SessionId = context.Session.SessionId,
            UserId = context.UserId,
            CorrelationId = context.CorrelationId,
            CurrentMessage = context.Harness.CurrentMessage,
            SelectedAgentKey = context.Metadata.TryGetValue("SelectedAgentKey", out var agent)
                ? agent?.ToString()
                : null,
            MemoryJson = JsonSerializer.Serialize(context.Memory.Memories, JsonOptions),
            ExecutionMetadata = new Dictionary<string, object?>(context.Metadata, StringComparer.OrdinalIgnoreCase),
            Status = WorkflowStatus.WaitingForApproval.ToString(),
            ApprovalRequestId = approvalRequestId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        await _store.SaveAsync(checkpoint, cancellationToken);

        await _streaming.PublishAsync(
            streamCtx,
            new CheckpointSavedEvent
            {
                CorrelationId = context.CorrelationId,
                CheckpointId = checkpoint.CheckpointId,
                WorkflowInstanceId = checkpoint.WorkflowInstanceId,
                ResumeStepIndex = resumeStepIndex
            },
            cancellationToken);

        _logger.LogInformation(
            "Checkpoint Created. CheckpointId={CheckpointId}, WorkflowInstanceId={InstanceId}, ResumeStepIndex={ResumeStepIndex}, CorrelationId={CorrelationId}",
            checkpoint.CheckpointId,
            checkpoint.WorkflowInstanceId,
            resumeStepIndex,
            context.CorrelationId);

        return checkpoint;
    }

    /// <inheritdoc />
    public async Task<WorkflowCheckpoint?> LoadAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = await _store.GetByInstanceIdAsync(workflowInstanceId, cancellationToken);
        if (checkpoint is null)
        {
            return null;
        }

        var streamCtx = _streaming.Current ?? new StreamingContext
        {
            CorrelationId = checkpoint.CorrelationId,
            SessionId = checkpoint.SessionId,
            UserId = checkpoint.UserId,
            WorkflowId = checkpoint.WorkflowDefinitionId
        };

        await _streaming.PublishAsync(
            streamCtx,
            new CheckpointLoadedEvent
            {
                CorrelationId = checkpoint.CorrelationId,
                CheckpointId = checkpoint.CheckpointId,
                WorkflowInstanceId = checkpoint.WorkflowInstanceId,
                ResumeStepIndex = checkpoint.ResumeStepIndex
            },
            cancellationToken);

        return checkpoint;
    }

    /// <inheritdoc />
    public Task MarkStatusAsync(
        Guid workflowInstanceId,
        WorkflowStatus status,
        CancellationToken cancellationToken = default) =>
        _store.UpdateStatusAsync(workflowInstanceId, status.ToString(), cancellationToken);
}

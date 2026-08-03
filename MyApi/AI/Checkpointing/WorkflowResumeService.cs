using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;
using MyApi.AI.AGUI;
using MyApi.AI.Harness;
using MyApi.AI.HITL;
using MyApi.AI.Memory;
using MyApi.AI.Sessions;
using MyApi.AI.Streaming;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Checkpointing;

/// <summary>
/// Resumes paused workflows from SQL checkpoints. Never re-runs completed steps.
/// </summary>
public sealed class WorkflowResumeService : IWorkflowResumeService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ICheckpointManager _checkpointManager;
    private readonly ICheckpointStore _checkpointStore;
    private readonly IApprovalService _approvalService;
    private readonly IWorkflowExecutor _workflowExecutor;
    private readonly IReadOnlyDictionary<string, IWorkflowDefinition> _definitions;
    private readonly ISessionStore _sessionStore;
    private readonly IMemoryRetrievalService _memoryRetrieval;
    private readonly IHarnessContextFactory _contextFactory;
    private readonly IStreamingService _streaming;
    private readonly ILogger<WorkflowResumeService> _logger;

    public WorkflowResumeService(
        ICheckpointManager checkpointManager,
        ICheckpointStore checkpointStore,
        IApprovalService approvalService,
        IWorkflowExecutor workflowExecutor,
        IEnumerable<IWorkflowDefinition> definitions,
        ISessionStore sessionStore,
        IMemoryRetrievalService memoryRetrieval,
        IHarnessContextFactory contextFactory,
        IStreamingService streaming,
        ILogger<WorkflowResumeService> logger)
    {
        _checkpointManager = checkpointManager;
        _checkpointStore = checkpointStore;
        _approvalService = approvalService;
        _workflowExecutor = workflowExecutor;
        _definitions = definitions.ToDictionary(d => d.WorkflowId, StringComparer.OrdinalIgnoreCase);
        _sessionStore = sessionStore;
        _memoryRetrieval = memoryRetrieval;
        _contextFactory = contextFactory;
        _streaming = streaming;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ResumeAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var checkpoint = await _checkpointManager.LoadAsync(workflowInstanceId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No checkpoint found for workflow instance '{workflowInstanceId}'.");

        if (!string.Equals(checkpoint.Status, WorkflowStatus.WaitingForApproval.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(checkpoint.Status, WorkflowStatus.Paused.ToString(), StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(checkpoint.Status, WorkflowStatus.Suspended.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Workflow instance '{workflowInstanceId}' is not resumable (status={checkpoint.Status}).");
        }

        var approval = await _approvalService.GetByWorkflowInstanceAsync(workflowInstanceId, cancellationToken);
        if (approval is not null && approval.Status != ApprovalStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot resume workflow '{workflowInstanceId}' until approval is granted (status={approval.Status}).");
        }

        var streamCtx = new StreamingContext
        {
            CorrelationId = checkpoint.CorrelationId,
            SessionId = checkpoint.SessionId,
            UserId = checkpoint.UserId,
            WorkflowId = checkpoint.WorkflowDefinitionId
        };

        using var scope = _streaming.BeginScope(streamCtx);
        using var registration = _streaming.RegisterExecution(checkpoint.CorrelationId);

        await _streaming.PublishAsync(
            streamCtx,
            new WorkflowResumedEvent
            {
                CorrelationId = checkpoint.CorrelationId,
                WorkflowInstanceId = workflowInstanceId,
                WorkflowId = checkpoint.WorkflowDefinitionId,
                ResumeStepIndex = checkpoint.ResumeStepIndex
            },
            cancellationToken);

        _logger.LogInformation(
            "Workflow Resumed. WorkflowInstanceId={InstanceId}, DefinitionId={DefinitionId}, ResumeStepIndex={ResumeStepIndex}, CorrelationId={CorrelationId}",
            workflowInstanceId,
            checkpoint.WorkflowDefinitionId,
            checkpoint.ResumeStepIndex,
            checkpoint.CorrelationId);

        var session = await _sessionStore.GetAsync(checkpoint.SessionId, cancellationToken)
            ?? await _sessionStore.CreateAsync(cancellationToken);

        var memory = await RebuildMemoryAsync(checkpoint, cancellationToken);
        var harness = _contextFactory.Create(
            checkpoint.UserId,
            session,
            checkpoint.CurrentMessage,
            memory,
            checkpoint.CorrelationId,
            cancellationToken);

        harness.PreparedMessages = BuildPreparedMessages(harness);

        var state = new WorkflowState
        {
            InstanceId = checkpoint.WorkflowInstanceId,
            WorkflowId = checkpoint.WorkflowDefinitionId,
            Status = WorkflowStatus.Running,
            CurrentStepIndex = checkpoint.ResumeStepIndex,
            StartedAt = DateTimeOffset.UtcNow
        };

        foreach (var (key, value) in checkpoint.Variables)
        {
            state.Set(key, value);
        }

        state.Set("CompletedSteps", checkpoint.CompletedSteps.ToList());

        var context = new WorkflowExecutionContext
        {
            Harness = harness,
            State = state,
            CancellationToken = cancellationToken
        };

        context.Metadata["ResumeFromIndex"] = checkpoint.ResumeStepIndex;
        context.Metadata["CompletedSteps"] = checkpoint.CompletedSteps.ToList();
        context.Metadata["SelectedAgentKey"] = checkpoint.SelectedAgentKey;
        context.Metadata["IsResume"] = true;
        foreach (var (key, value) in checkpoint.ExecutionMetadata)
        {
            context.Metadata.TryAdd(key, value);
        }

        if (!_definitions.TryGetValue(checkpoint.WorkflowDefinitionId, out var definition))
        {
            throw new InvalidOperationException(
                $"Workflow definition '{checkpoint.WorkflowDefinitionId}' is not registered.");
        }

        context.Metadata["WorkflowId"] = definition.WorkflowId;
        context.Metadata["WorkflowName"] = definition.WorkflowName;

        var result = await _workflowExecutor.ExecuteAsync(definition, context, cancellationToken);

        sw.Stop();

        if (result.Succeeded)
        {
            await _checkpointManager.MarkStatusAsync(
                workflowInstanceId,
                WorkflowStatus.Completed,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                session.AddAssistantMessage(result.Output);
                await _sessionStore.UpdateAsync(session, cancellationToken);
            }

            _logger.LogInformation(
                "Workflow Completed after resume. WorkflowInstanceId={InstanceId}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
                workflowInstanceId,
                sw.ElapsedMilliseconds,
                checkpoint.CorrelationId);
        }
        else if (result.Status == WorkflowStatus.WaitingForApproval)
        {
            _logger.LogInformation(
                "Workflow paused again after resume (nested approval). WorkflowInstanceId={InstanceId}",
                workflowInstanceId);
        }
        else
        {
            await _checkpointManager.MarkStatusAsync(
                workflowInstanceId,
                result.Status,
                cancellationToken);
        }

        return new WorkflowExecutionResult
        {
            WorkflowId = result.WorkflowId,
            WorkflowName = result.WorkflowName,
            Status = result.Status,
            Output = result.Output,
            Data = result.Data,
            Duration = sw.Elapsed,
            CorrelationId = result.CorrelationId,
            Error = result.Error,
            WorkflowInstanceId = result.WorkflowInstanceId ?? workflowInstanceId,
            ApprovalRequestId = result.ApprovalRequestId,
            CheckpointId = result.CheckpointId
        };
    }

    /// <inheritdoc />
    public async Task<WorkflowInstanceStatusDto?> GetStatusAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var checkpoint = await _checkpointStore.GetByInstanceIdAsync(workflowInstanceId, cancellationToken);
        if (checkpoint is null)
        {
            return null;
        }

        return new WorkflowInstanceStatusDto
        {
            WorkflowInstanceId = checkpoint.WorkflowInstanceId,
            WorkflowDefinitionId = checkpoint.WorkflowDefinitionId,
            WorkflowName = checkpoint.WorkflowName,
            Status = checkpoint.Status,
            ResumeStepIndex = checkpoint.ResumeStepIndex,
            CompletedSteps = checkpoint.CompletedSteps,
            ApprovalRequestId = checkpoint.ApprovalRequestId,
            SessionId = checkpoint.SessionId,
            UserId = checkpoint.UserId,
            CorrelationId = checkpoint.CorrelationId,
            UpdatedAt = checkpoint.UpdatedAt
        };
    }

    private async Task<MemoryContext> RebuildMemoryAsync(
        WorkflowCheckpoint checkpoint,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(checkpoint.MemoryJson))
            {
                var records = JsonSerializer.Deserialize<List<MemoryRecord>>(checkpoint.MemoryJson, JsonOptions);
                if (records is { Count: > 0 })
                {
                    return new MemoryContext(checkpoint.UserId, records);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize memory snapshot; re-retrieving.");
        }

        return await _memoryRetrieval.RetrieveAsync(
            checkpoint.UserId,
            checkpoint.CurrentMessage,
            cancellationToken);
    }

    private static IReadOnlyList<ChatMessage> BuildPreparedMessages(HarnessContext context)
    {
        var messages = new List<ChatMessage>();
        var memoryFragment = context.Memory.ToSystemPromptFragment();
        if (!string.IsNullOrWhiteSpace(memoryFragment))
        {
            messages.Add(new ChatMessage(ChatRole.System, memoryFragment));
        }

        foreach (var prior in context.ConversationHistory)
        {
            messages.Add(prior.Role switch
            {
                SessionMessageRole.Assistant => new ChatMessage(ChatRole.Assistant, prior.Content),
                SessionMessageRole.System => new ChatMessage(ChatRole.System, prior.Content),
                _ => new ChatMessage(ChatRole.User, prior.Content)
            });
        }

        messages.Add(new ChatMessage(ChatRole.User, context.CurrentMessage));
        return messages;
    }
}

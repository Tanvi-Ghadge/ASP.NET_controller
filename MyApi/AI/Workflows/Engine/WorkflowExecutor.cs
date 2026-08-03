using System.Diagnostics;
using MyApi.AI.AGUI;
using MyApi.AI.Checkpointing;
using MyApi.AI.Observability;
using MyApi.AI.Streaming;
using MyApi.AI.Workflows.Models;
using MyApi.AI.Workflows.Steps;

namespace MyApi.AI.Workflows.Engine;

/// <summary>
/// Sequentially executes workflow steps with HITL pause, checkpoint, and resume support.
/// </summary>
public interface IWorkflowExecutor
{
    /// <summary>Runs all steps for the given definition (or continues from resume metadata).</summary>
    Task<WorkflowExecutionResult> ExecuteAsync(
        IWorkflowDefinition definition,
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default sequential workflow executor with durable pause/resume.
/// </summary>
public sealed class WorkflowExecutor : IWorkflowExecutor
{
    private readonly IStreamingService _streaming;
    private readonly ICheckpointManager _checkpointManager;
    private readonly ILogger<WorkflowExecutor> _logger;

    public WorkflowExecutor(
        IStreamingService streaming,
        ICheckpointManager checkpointManager,
        ILogger<WorkflowExecutor> logger)
    {
        _streaming = streaming;
        _checkpointManager = checkpointManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        IWorkflowDefinition definition,
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(context);

        var sw = Stopwatch.StartNew();
        var state = context.State;
        state.Status = WorkflowStatus.Running;
        state.StartedAt ??= DateTimeOffset.UtcNow;
        state.WorkflowId = definition.WorkflowId;

        var streamCtx = _streaming.Current ?? new StreamingContext
        {
            CorrelationId = context.CorrelationId,
            SessionId = context.Session.SessionId,
            UserId = context.UserId,
            WorkflowId = definition.WorkflowId
        };
        streamCtx.WorkflowId = definition.WorkflowId;

        using var activity = AiActivitySource.StartActivity("ai.workflow.execute");
        activity?.SetTag("workflow.id", definition.WorkflowId);
        activity?.SetTag("workflow.name", definition.WorkflowName);
        activity?.SetTag("correlation.id", context.CorrelationId);
        activity?.SetTag("workflow.instance.id", state.InstanceId.ToString());

        var startIndex = ResolveStartIndex(context);
        var completedSteps = ResolveCompletedSteps(context);

        _logger.LogInformation(
            "Workflow Started. WorkflowName={WorkflowName}, WorkflowId={WorkflowId}, InstanceId={InstanceId}, StartIndex={StartIndex}, CompletedSteps={CompletedCount}, CorrelationId={CorrelationId}",
            definition.WorkflowName,
            definition.WorkflowId,
            state.InstanceId,
            startIndex,
            completedSteps.Count,
            context.CorrelationId);

        try
        {
            for (var i = startIndex; i < definition.Steps.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var step = definition.Steps[i];
                state.CurrentStepIndex = i;
                state.CurrentStepName = step.Name;

                if (completedSteps.Contains(step.Name))
                {
                    _logger.LogInformation(
                        "Step Skipped (already completed). WorkflowName={WorkflowName}, CurrentStep={StepName}, CorrelationId={CorrelationId}",
                        definition.WorkflowName,
                        step.Name,
                        context.CorrelationId);

                    await _streaming.PublishAsync(
                        streamCtx,
                        WorkflowEvent.StepSkipped(
                            context.CorrelationId,
                            definition.WorkflowId,
                            step.Name,
                            "Already completed before pause/resume."),
                        cancellationToken);
                    continue;
                }

                await _streaming.PublishAsync(
                    streamCtx,
                    WorkflowEvent.StepStarted(
                        context.CorrelationId,
                        definition.WorkflowId,
                        step.Name,
                        i),
                    cancellationToken);

                _logger.LogInformation(
                    "Step Started. WorkflowName={WorkflowName}, CurrentStep={StepName}, StepIndex={StepIndex}, CorrelationId={CorrelationId}",
                    definition.WorkflowName,
                    step.Name,
                    i,
                    context.CorrelationId);

                var stepSw = Stopwatch.StartNew();
                StepResult stepResult;
                try
                {
                    stepResult = await step.ExecuteAsync(context, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    state.Status = WorkflowStatus.Cancelled;
                    state.LastError = "Cancelled";
                    state.CompletedAt = DateTimeOffset.UtcNow;
                    sw.Stop();
                    return Fail(definition, context, state, sw.Elapsed, "Cancelled", WorkflowStatus.Cancelled);
                }
                catch (Exception ex)
                {
                    stepSw.Stop();
                    state.Status = WorkflowStatus.Failed;
                    state.LastError = ex.Message;
                    state.CompletedAt = DateTimeOffset.UtcNow;
                    sw.Stop();

                    await _streaming.PublishAsync(
                        streamCtx,
                        WorkflowEvent.StepFailed(
                            context.CorrelationId,
                            definition.WorkflowId,
                            step.Name,
                            ex.Message,
                            stepSw.ElapsedMilliseconds),
                        cancellationToken);

                    return Fail(definition, context, state, sw.Elapsed, ex.Message, WorkflowStatus.Failed);
                }

                stepSw.Stop();

                switch (stepResult.Status)
                {
                    case StepStatus.Success:
                        if (stepResult.Output is not null)
                        {
                            state.Set($"step:{step.Name}:output", stepResult.Output);
                        }

                        completedSteps.Add(step.Name);
                        state.Set("CompletedSteps", completedSteps.ToList());

                        await _streaming.PublishAsync(
                            streamCtx,
                            WorkflowEvent.StepCompleted(
                                context.CorrelationId,
                                definition.WorkflowId,
                                step.Name,
                                stepSw.ElapsedMilliseconds,
                                stepResult.Message),
                            cancellationToken);
                        break;

                    case StepStatus.Skip:
                        completedSteps.Add(step.Name);
                        state.Set("CompletedSteps", completedSteps.ToList());
                        await _streaming.PublishAsync(
                            streamCtx,
                            WorkflowEvent.StepSkipped(
                                context.CorrelationId,
                                definition.WorkflowId,
                                step.Name,
                                stepResult.Message),
                            cancellationToken);
                        break;

                    case StepStatus.WaitingForApproval:
                        completedSteps.Add(step.Name);
                        state.Set("CompletedSteps", completedSteps.ToList());

                        var approvalRequestId = state.Get<Guid?>("ApprovalRequestId")
                            ?? TryReadGuid(stepResult.Output);

                        var checkpoint = await _checkpointManager.SavePauseCheckpointAsync(
                            context,
                            definition,
                            resumeStepIndex: i + 1,
                            completedSteps: completedSteps.ToList(),
                            approvalRequestId: approvalRequestId,
                            cancellationToken: cancellationToken);

                        state.Status = WorkflowStatus.WaitingForApproval;
                        state.CompletedAt = DateTimeOffset.UtcNow;
                        sw.Stop();

                        var pauseMessage = stepResult.Message ?? "Approval Required";
                        state.Set("FinalResponse", pauseMessage);

                        await _streaming.PublishAsync(
                            streamCtx,
                            new WorkflowPausedEvent
                            {
                                CorrelationId = context.CorrelationId,
                                WorkflowInstanceId = state.InstanceId,
                                WorkflowId = definition.WorkflowId,
                                Reason = pauseMessage,
                                ResumeStepIndex = i + 1
                            },
                            cancellationToken);

                        if (approvalRequestId is Guid aid)
                        {
                            await _streaming.PublishAsync(
                                streamCtx,
                                new ApprovalRequestedEvent
                                {
                                    CorrelationId = context.CorrelationId,
                                    ApprovalRequestId = aid,
                                    WorkflowInstanceId = state.InstanceId,
                                    Title = state.Get<string>("ApprovalTitle") ?? "Approval Required"
                                },
                                cancellationToken);
                        }

                        _logger.LogInformation(
                            "Workflow Paused. WorkflowName={WorkflowName}, InstanceId={InstanceId}, ResumeStepIndex={ResumeStepIndex}, CheckpointId={CheckpointId}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
                            definition.WorkflowName,
                            state.InstanceId,
                            i + 1,
                            checkpoint.CheckpointId,
                            sw.ElapsedMilliseconds,
                            context.CorrelationId);

                        return new WorkflowExecutionResult
                        {
                            WorkflowId = definition.WorkflowId,
                            WorkflowName = definition.WorkflowName,
                            Status = WorkflowStatus.WaitingForApproval,
                            Output = pauseMessage,
                            Data = state.Variables,
                            Duration = sw.Elapsed,
                            CorrelationId = context.CorrelationId,
                            WorkflowInstanceId = state.InstanceId,
                            ApprovalRequestId = approvalRequestId,
                            CheckpointId = checkpoint.CheckpointId
                        };

                    case StepStatus.Retry:
                        state.Status = WorkflowStatus.Failed;
                        state.LastError = stepResult.Message ?? "Step requested retry";
                        state.CompletedAt = DateTimeOffset.UtcNow;
                        sw.Stop();
                        return Fail(definition, context, state, sw.Elapsed, state.LastError, WorkflowStatus.Failed);

                    case StepStatus.Cancelled:
                        state.Status = WorkflowStatus.Cancelled;
                        state.LastError = stepResult.Message;
                        state.CompletedAt = DateTimeOffset.UtcNow;
                        sw.Stop();
                        return Fail(definition, context, state, sw.Elapsed, stepResult.Message ?? "Cancelled", WorkflowStatus.Cancelled);

                    case StepStatus.Failure:
                    default:
                        state.Status = WorkflowStatus.Failed;
                        state.LastError = stepResult.Message ?? "Step failed";
                        state.CompletedAt = DateTimeOffset.UtcNow;
                        sw.Stop();
                        await _streaming.PublishAsync(
                            streamCtx,
                            WorkflowEvent.StepFailed(
                                context.CorrelationId,
                                definition.WorkflowId,
                                step.Name,
                                state.LastError,
                                stepSw.ElapsedMilliseconds),
                            cancellationToken);
                        return Fail(definition, context, state, sw.Elapsed, state.LastError, WorkflowStatus.Failed);
                }
            }

            state.Status = WorkflowStatus.Completed;
            state.CompletedAt = DateTimeOffset.UtcNow;
            sw.Stop();

            var output = state.Get<string>("FinalResponse")
                ?? state.Get<string>("ReportText")
                ?? state.Get<string>("EmployeeSummary")
                ?? state.Get<string>("AgentResponse")
                ?? "Workflow completed.";

            _logger.LogInformation(
                "Workflow Completed. WorkflowName={WorkflowName}, InstanceId={InstanceId}, WorkflowDurationMs={DurationMs}, CorrelationId={CorrelationId}",
                definition.WorkflowName,
                state.InstanceId,
                sw.ElapsedMilliseconds,
                context.CorrelationId);

            return new WorkflowExecutionResult
            {
                WorkflowId = definition.WorkflowId,
                WorkflowName = definition.WorkflowName,
                Status = WorkflowStatus.Completed,
                Output = output,
                Data = state.Variables,
                Duration = sw.Elapsed,
                CorrelationId = context.CorrelationId,
                WorkflowInstanceId = state.InstanceId
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            state.Status = WorkflowStatus.Failed;
            state.LastError = ex.Message;
            state.CompletedAt = DateTimeOffset.UtcNow;
            _logger.LogError(
                ex,
                "Workflow Failed. WorkflowName={WorkflowName}, CorrelationId={CorrelationId}",
                definition.WorkflowName,
                context.CorrelationId);
            return Fail(definition, context, state, sw.Elapsed, ex.Message, WorkflowStatus.Failed);
        }
    }

    private static int ResolveStartIndex(WorkflowExecutionContext context)
    {
        if (context.Metadata.TryGetValue("ResumeFromIndex", out var value))
        {
            if (value is int i)
            {
                return i;
            }

            if (value is long l)
            {
                return (int)l;
            }

            if (int.TryParse(value?.ToString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static HashSet<string> ResolveCompletedSteps(WorkflowExecutionContext context)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (context.Metadata.TryGetValue("CompletedSteps", out var meta) && meta is IEnumerable<string> fromMeta)
        {
            foreach (var name in fromMeta)
            {
                set.Add(name);
            }
        }

        var fromState = context.State.Get<object>("CompletedSteps");
        if (fromState is IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                set.Add(name);
            }
        }
        else if (fromState is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item is not null)
                {
                    set.Add(item.ToString()!);
                }
            }
        }

        return set;
    }

    private static Guid? TryReadGuid(object? output)
    {
        if (output is Guid g)
        {
            return g;
        }

        if (output is string s && Guid.TryParse(s, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static WorkflowExecutionResult Fail(
        IWorkflowDefinition definition,
        WorkflowExecutionContext context,
        WorkflowState state,
        TimeSpan duration,
        string error,
        WorkflowStatus status) =>
        new()
        {
            WorkflowId = definition.WorkflowId,
            WorkflowName = definition.WorkflowName,
            Status = status,
            Output = string.Empty,
            Data = state.Variables,
            Duration = duration,
            CorrelationId = context.CorrelationId,
            Error = error,
            WorkflowInstanceId = state.InstanceId
        };
}

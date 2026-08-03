using System.Diagnostics;
using MyApi.AI.AGUI;
using MyApi.AI.AgentRouter;
using MyApi.AI.Streaming;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Orchestration;

/// <summary>
/// Enterprise orchestrator: classify intent → select workflow/agent via router → build plan → invoke engine.
/// Publishes AG-UI planning / agent-selected events via <see cref="IStreamingService"/>.
/// </summary>
public sealed class EnterpriseOrchestrator : IOrchestrator
{
    private readonly IIntentClassifier _intentClassifier;
    private readonly IWorkflowSelector _workflowSelector;
    private readonly IAgentSelector _agentSelector;
    private readonly IAgentRouter _agentRouter;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IStreamingService _streaming;
    private readonly ILogger<EnterpriseOrchestrator> _logger;

    public EnterpriseOrchestrator(
        IIntentClassifier intentClassifier,
        IWorkflowSelector workflowSelector,
        IAgentSelector agentSelector,
        IAgentRouter agentRouter,
        IWorkflowEngine workflowEngine,
        IStreamingService streaming,
        ILogger<EnterpriseOrchestrator> logger)
    {
        _intentClassifier = intentClassifier;
        _workflowSelector = workflowSelector;
        _agentSelector = agentSelector;
        _agentRouter = agentRouter;
        _workflowEngine = workflowEngine;
        _streaming = streaming;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OrchestrationResult> OrchestrateAsync(
        OrchestrationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sw = Stopwatch.StartNew();
        var streamCtx = _streaming.Current ?? new StreamingContext
        {
            CorrelationId = context.CorrelationId,
            SessionId = context.Harness.Session.SessionId,
            UserId = context.UserId
        };

        _logger.LogInformation(
            "Incoming Request. CorrelationId={CorrelationId}, UserId={UserId}, MessageLength={Length}",
            context.CorrelationId,
            context.UserId,
            context.UserMessage.Length);

        var intent = await _intentClassifier.ClassifyAsync(context.UserMessage, cancellationToken);
        _logger.LogInformation(
            "Detected Intent={Intent}. CorrelationId={CorrelationId}",
            intent,
            context.CorrelationId);

        var workflow = _workflowSelector.Select(intent);
        streamCtx.WorkflowId = workflow.WorkflowId;
        _logger.LogInformation(
            "Selected Workflow={WorkflowName} ({WorkflowId}). CorrelationId={CorrelationId}",
            workflow.WorkflowName,
            workflow.WorkflowId,
            context.CorrelationId);

        await _streaming.PublishAsync(
            streamCtx,
            new WorkflowStartedEvent
            {
                CorrelationId = context.CorrelationId,
                WorkflowId = workflow.WorkflowId,
                WorkflowName = workflow.WorkflowName,
                StepCount = workflow.EstimatedSteps
            },
            cancellationToken);

        var agentSelection = _agentSelector.Select(intent, workflow.WorkflowId);
        var resolvedAgent = _agentRouter.Resolve(agentSelection.AgentKey);

        await _streaming.PublishAsync(
            streamCtx,
            new AgentSelectedEvent
            {
                CorrelationId = context.CorrelationId,
                AgentKey = resolvedAgent.AgentKey,
                AgentName = resolvedAgent.AgentName,
                Domain = resolvedAgent.Domain
            },
            cancellationToken);

        _logger.LogInformation(
            "Agent Selected. Agent={AgentName} ({AgentKey}), Domain={Domain}. CorrelationId={CorrelationId}",
            resolvedAgent.AgentName,
            resolvedAgent.AgentKey,
            resolvedAgent.Domain,
            context.CorrelationId);

        var emailRequested = WantsEmail(context.UserMessage);
        var strategy = ExecutionStrategy.Sequential;

        var plan = new OrchestrationPlan
        {
            DetectedIntent = intent,
            SelectedWorkflowId = workflow.WorkflowId,
            SelectedWorkflowName = workflow.WorkflowName,
            SelectedAgentKey = resolvedAgent.AgentKey,
            ExecutionStrategy = strategy,
            EstimatedSteps = workflow.EstimatedSteps,
            WorkflowIds = [workflow.WorkflowId],
            Rationale =
                $"Intent {intent} mapped to workflow {workflow.WorkflowId} with agent {resolvedAgent.AgentKey} ({resolvedAgent.Domain}).",
            Metadata =
            {
                ["AgentName"] = resolvedAgent.AgentName,
                ["Domain"] = resolvedAgent.Domain,
                ["EmailRequested"] = emailRequested,
                ["SupportsParallel"] = false,
                ["SupportsA2A"] = true
            }
        };

        context.Harness.Metadata["OrchestrationPlanId"] = plan.PlanId;
        context.Harness.Metadata["DetectedIntent"] = intent.ToString();
        context.Harness.Metadata["SelectedAgentKey"] = resolvedAgent.AgentKey;
        context.Harness.Metadata["EmailRequested"] = emailRequested;

        try
        {
            var executionResult = await ExecutePlanAsync(plan, context, emailRequested, cancellationToken);
            sw.Stop();

            if (executionResult.Status == WorkflowStatus.Completed)
            {
                await _streaming.PublishAsync(
                    streamCtx,
                    WorkflowEvent.Completed(
                        context.CorrelationId,
                        workflow.WorkflowId,
                        workflow.WorkflowName,
                        executionResult.Status.ToString(),
                        sw.ElapsedMilliseconds),
                    cancellationToken);
            }

            var approvalPending = executionResult.ApprovalPending;
            return new OrchestrationResult
            {
                Plan = plan,
                Output = approvalPending
                    ? executionResult.Output
                    : (executionResult.Succeeded
                        ? executionResult.Output
                        : (executionResult.Error ?? "Workflow execution failed.")),
                Succeeded = executionResult.Succeeded,
                ApprovalPending = approvalPending,
                WorkflowInstanceId = executionResult.WorkflowInstanceId,
                ApprovalRequestId = executionResult.ApprovalRequestId,
                Error = approvalPending ? null : executionResult.Error,
                Duration = sw.Elapsed,
                CorrelationId = context.CorrelationId,
                Data = executionResult.Data
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Orchestration failed. PlanId={PlanId}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
                plan.PlanId,
                sw.ElapsedMilliseconds,
                context.CorrelationId);

            await _streaming.PublishAsync(
                streamCtx,
                new ErrorEvent
                {
                    CorrelationId = context.CorrelationId,
                    Message = ex.Message,
                    Source = "Orchestrator"
                },
                cancellationToken);

            return new OrchestrationResult
            {
                Plan = plan,
                Output = string.Empty,
                Succeeded = false,
                Error = ex.Message,
                Duration = sw.Elapsed,
                CorrelationId = context.CorrelationId
            };
        }
    }

    private async Task<WorkflowExecutionResult> ExecutePlanAsync(
        OrchestrationPlan plan,
        OrchestrationContext context,
        bool emailRequested,
        CancellationToken cancellationToken) =>
        plan.ExecutionStrategy switch
        {
            ExecutionStrategy.Sequential =>
                await ExecuteSequentialAsync(plan, context, emailRequested, cancellationToken),
            ExecutionStrategy.Parallel => throw new NotSupportedException(
                "Parallel orchestration is designed but not implemented yet."),
            ExecutionStrategy.Conditional => throw new NotSupportedException(
                "Conditional orchestration is designed but not implemented yet."),
            _ => await ExecuteSequentialAsync(plan, context, emailRequested, cancellationToken)
        };

    private async Task<WorkflowExecutionResult> ExecuteSequentialAsync(
        OrchestrationPlan plan,
        OrchestrationContext context,
        bool emailRequested,
        CancellationToken cancellationToken)
    {
        WorkflowExecutionResult? last = null;

        foreach (var workflowId in plan.WorkflowIds)
        {
            var workflowContext = new WorkflowExecutionContext
            {
                Harness = context.Harness,
                State = new WorkflowState { WorkflowId = workflowId },
                CancellationToken = cancellationToken
            };

            workflowContext.Metadata["OrchestrationPlanId"] = plan.PlanId;
            workflowContext.Metadata["SelectedAgentKey"] = plan.SelectedAgentKey;
            workflowContext.Metadata["DetectedIntent"] = plan.DetectedIntent.ToString();
            workflowContext.Metadata["EmailRequested"] = emailRequested;

            last = await _workflowEngine.ExecuteAsync(workflowId, workflowContext, cancellationToken);
            if (!last.Succeeded)
            {
                return last;
            }
        }

        return last ?? new WorkflowExecutionResult
        {
            WorkflowId = plan.SelectedWorkflowId,
            WorkflowName = plan.SelectedWorkflowName,
            Status = WorkflowStatus.Failed,
            Output = string.Empty,
            Error = "No workflows were executed.",
            CorrelationId = context.CorrelationId
        };
    }

    private static bool WantsEmail(string message) =>
        ContainsAny(message, "email", "e-mail", "sms", "notify", "notification", "email hr", "send to hr");

    private static bool ContainsAny(string text, params string[] tokens) =>
        tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}

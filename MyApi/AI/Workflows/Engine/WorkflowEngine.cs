using MyApi.AI.Checkpointing;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Engine;

/// <summary>
/// Default workflow engine that resolves definitions from DI and delegates to <see cref="IWorkflowExecutor"/>.
/// </summary>
public sealed class WorkflowEngine : IWorkflowEngine
{
    private readonly IReadOnlyDictionary<string, IWorkflowDefinition> _definitions;
    private readonly IWorkflowExecutor _executor;
    private readonly IWorkflowResumeService _resumeService;
    private readonly ICheckpointManager _checkpointManager;
    private readonly ILogger<WorkflowEngine> _logger;

    public WorkflowEngine(
        IEnumerable<IWorkflowDefinition> definitions,
        IWorkflowExecutor executor,
        IWorkflowResumeService resumeService,
        ICheckpointManager checkpointManager,
        ILogger<WorkflowEngine> logger)
    {
        _executor = executor;
        _resumeService = resumeService;
        _checkpointManager = checkpointManager;
        _logger = logger;
        _definitions = definitions.ToDictionary(d => d.WorkflowId, StringComparer.OrdinalIgnoreCase);
        RegisteredWorkflowIds = _definitions.Keys.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> RegisteredWorkflowIds { get; }

    /// <inheritdoc />
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        string workflowId,
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowId);
        ArgumentNullException.ThrowIfNull(context);

        if (!_definitions.TryGetValue(workflowId, out var definition))
        {
            throw new InvalidOperationException(
                $"Workflow '{workflowId}' is not registered. Known: {string.Join(", ", RegisteredWorkflowIds)}");
        }

        context.Metadata["WorkflowId"] = definition.WorkflowId;
        context.Metadata["WorkflowName"] = definition.WorkflowName;
        context.State.WorkflowId = definition.WorkflowId;

        _logger.LogInformation(
            "WorkflowEngine executing {WorkflowName} ({WorkflowId}). InstanceId={InstanceId}, CorrelationId={CorrelationId}",
            definition.WorkflowName,
            definition.WorkflowId,
            context.State.InstanceId,
            context.CorrelationId);

        return await _executor.ExecuteAsync(definition, context, cancellationToken);
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionResult> ResumeAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("WorkflowEngine.ResumeAsync({InstanceId})", workflowInstanceId);
        return _resumeService.ResumeAsync(workflowInstanceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task CancelAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Workflow Cancelled. WorkflowInstanceId={InstanceId}", workflowInstanceId);
        await _checkpointManager.MarkStatusAsync(workflowInstanceId, WorkflowStatus.Cancelled, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SuspendAsync(Guid workflowInstanceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Workflow Suspended. WorkflowInstanceId={InstanceId}", workflowInstanceId);
        await _checkpointManager.MarkStatusAsync(workflowInstanceId, WorkflowStatus.Suspended, cancellationToken);
    }
}

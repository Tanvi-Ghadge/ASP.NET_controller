using MyApi.AI.Plugins;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Deletes the employee after HITL approval. Uses employee id from workflow state (never re-validates from scratch).
/// </summary>
public sealed class DeleteEmployeeStep : IWorkflowStep
{
    private readonly EmployeePlugin _employeePlugin;
    private readonly ILogger<DeleteEmployeeStep> _logger;

    public DeleteEmployeeStep(
        EmployeePlugin employeePlugin,
        ILogger<DeleteEmployeeStep> logger)
    {
        _employeePlugin = employeePlugin;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "DeleteEmployee";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var employeeId = ResolveEmployeeId(context);
        if (employeeId is null)
        {
            return StepResult.Failure("Could not determine which employee id to delete.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _employeePlugin.DeleteEmployee(employeeId.Value);
        context.State.Set("FinalResponse", result);
        context.State.Set("DeleteResult", result);

        _logger.LogInformation(
            "DeleteEmployeeStep completed for {EmployeeId}. CorrelationId={CorrelationId}",
            employeeId,
            context.CorrelationId);

        return StepResult.Success(result, result);
    }

    private static int? ResolveEmployeeId(WorkflowExecutionContext context)
    {
        var typed = context.State.Get<int?>(ValidateEmployeeDeleteStep.EmployeeIdKey);
        if (typed is not null)
        {
            return typed;
        }

        if (context.State.Variables.TryGetValue(ValidateEmployeeDeleteStep.EmployeeIdKey, out var raw) &&
            raw is not null &&
            int.TryParse(raw.ToString(), out var parsed))
        {
            return parsed;
        }

        return null;
    }
}

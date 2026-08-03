using MyApi.AI.Plugins;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Sends a notification after a successful delete (post-approval path).
/// </summary>
public sealed class NotifyDeleteStep : IWorkflowStep
{
    private readonly NotificationPlugin _notificationPlugin;
    private readonly ILogger<NotifyDeleteStep> _logger;

    public NotifyDeleteStep(
        NotificationPlugin notificationPlugin,
        ILogger<NotifyDeleteStep> logger)
    {
        _notificationPlugin = notificationPlugin;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "NotifyDelete";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var deleteResult = context.State.Get<string>("DeleteResult")
            ?? context.State.Get<string>("FinalResponse")
            ?? "Employee deleted.";

        var employeeId = context.State.Variables.TryGetValue(ValidateEmployeeDeleteStep.EmployeeIdKey, out var raw)
            ? raw?.ToString()
            : "?";

        cancellationToken.ThrowIfCancellationRequested();
        var note = await _notificationPlugin.SendEmailNotification(
            "hr@company.com",
            $"Employee {employeeId} deleted",
            deleteResult);

        var combined = $"{deleteResult}{Environment.NewLine}{Environment.NewLine}{note}";
        context.State.Set("FinalResponse", combined);
        context.State.Set("NotificationResult", note);

        _logger.LogInformation(
            "NotifyDeleteStep completed. EmployeeId={EmployeeId}, CorrelationId={CorrelationId}",
            employeeId,
            context.CorrelationId);

        return StepResult.Success(note, note);
    }
}

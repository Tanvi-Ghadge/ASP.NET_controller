using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Placeholder for future report email delivery (Hangfire/email service).
/// Currently skipped so the workflow remains complete without side effects.
/// </summary>
public sealed class EmailReportStep : IWorkflowStep
{
    private readonly ILogger<EmailReportStep> _logger;

    public EmailReportStep(ILogger<EmailReportStep> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "EmailReport";

    /// <inheritdoc />
    public Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "EmailReportStep skipped (not configured). CorrelationId={CorrelationId}",
            context.CorrelationId);

        return Task.FromResult(
            StepResult.Skip("Email delivery is not configured yet; report returned in-chat."));
    }
}

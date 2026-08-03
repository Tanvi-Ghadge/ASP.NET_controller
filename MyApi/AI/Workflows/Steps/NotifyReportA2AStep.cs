using System.Text.RegularExpressions;
using MyApi.AI.A2A;
using MyApi.AI.Agents.Notification;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Emails the generated report via A2A to <see cref="NotificationAgent"/> when requested.
/// </summary>
public sealed partial class NotifyReportA2AStep : IWorkflowStep
{
    private readonly IAgentCommunication _a2a;
    private readonly ILogger<NotifyReportA2AStep> _logger;

    public NotifyReportA2AStep(IAgentCommunication a2a, ILogger<NotifyReportA2AStep> logger)
    {
        _a2a = a2a;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "NotifyReportA2A";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var emailRequested = IsEmailRequested(context);
        if (!emailRequested)
        {
            _logger.LogInformation(
                "Notification skipped — email not requested. CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
                context.CorrelationId,
                context.State.WorkflowId);
            return StepResult.Skip("Email/notification was not requested.");
        }

        var report = context.State.Get<string>(GeneratePayrollReportA2AStep.ReportTextKey)
            ?? context.State.Get<string>("FinalResponse")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(report))
        {
            return StepResult.Failure("No report available to email.");
        }

        var recipient = ResolveRecipient(context.Harness.CurrentMessage);

        var response = await _a2a.SendAsync(
            new AgentRequest
            {
                AgentName = NotificationAgent.Key,
                Operation = "SendEmail",
                CorrelationId = context.CorrelationId,
                SessionId = context.Session.SessionId,
                MemoryContext = context.Memory,
                RequestData = new Dictionary<string, object?>
                {
                    ["email"] = recipient,
                    ["subject"] = "Employee Payroll Report",
                    ["body"] = report
                },
                ExecutionMetadata =
                {
                    ["WorkflowId"] = context.State.WorkflowId,
                    ["Step"] = Name
                }
            },
            cancellationToken);

        if (!response.Succeeded)
        {
            var error = response.Errors.FirstOrDefault() ?? "NotificationAgent failed.";
            return StepResult.Failure(error);
        }

        var note = response.TextOutput ?? $"Report emailed to {recipient}.";
        var combined = $"{report}{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}{note}";
        context.State.Set("FinalResponse", combined);
        context.State.Set("NotificationResult", note);

        _logger.LogInformation(
            "Report emailed via A2A. Recipient={Recipient}, CorrelationId={CorrelationId}, WorkflowId={WorkflowId}",
            recipient,
            context.CorrelationId,
            context.State.WorkflowId);

        return StepResult.Success(note, note);
    }

    private static bool IsEmailRequested(WorkflowExecutionContext context)
    {
        if (context.Metadata.TryGetValue("EmailRequested", out var flag) && flag is true)
        {
            return true;
        }

        var message = context.Harness.CurrentMessage;
        return ContainsAny(message, "email", "e-mail", "sms", "notify", "notification", "send to hr", "email hr");
    }

    private static string ResolveRecipient(string message)
    {
        var match = EmailRegex().Match(message);
        if (match.Success)
        {
            return match.Value;
        }

        return "hr@company.com";
    }

    private static bool ContainsAny(string text, params string[] tokens) =>
        tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));

    [GeneratedRegex(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();
}

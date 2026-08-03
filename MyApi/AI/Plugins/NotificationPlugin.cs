using System.ComponentModel;
using Microsoft.Extensions.AI;
using MyApi.Service.Interface;

namespace MyApi.AI.Plugins;

/// <summary>
/// Notification-domain tools. Uses existing <see cref="Iemailservice"/> — no direct SQL.
/// </summary>
public sealed class NotificationPlugin
{
    private readonly Iemailservice _emailService;
    private readonly ILogger<NotificationPlugin> _logger;

    public NotificationPlugin(Iemailservice emailService, ILogger<NotificationPlugin> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public IList<AITool> GetAITools() =>
    [
        AIFunctionFactory.Create(SendEmailNotification)
    ];

    [Description("Sends an email notification to a recipient.")]
    public async Task<string> SendEmailNotification(
        [Description("Recipient email address.")] string email,
        [Description("Email subject.")] string subject,
        [Description("Email body.")] string body)
    {
        _logger.LogInformation("NotificationPlugin.SendEmailNotification(email={Email})", email);
        await _emailService.SendNotificationEmail(email, subject, body);
        return $"Notification email queued for {email} with subject '{subject}'.";
    }
}

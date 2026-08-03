using System;

namespace MyApi.Service.Interface;

public interface Iemailservice
{
    Task SendWelcomeEmail(string email, string name);

    /// <summary>Sends a generic notification email (used by NotificationAgent).</summary>
    Task SendNotificationEmail(string email, string subject, string body);
}

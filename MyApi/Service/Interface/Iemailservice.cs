using System;

namespace MyApi.Service.Interface;

public interface Iemailservice
{
    Task SendWelcomeEmail(string email, string name);
}

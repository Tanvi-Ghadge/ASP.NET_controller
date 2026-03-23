using System;
using Microsoft.Extensions.Logging;
using MyApi.Service.Interface;
namespace MyApi.Service.Implementation;

public class Emailservice: Iemailservice
{
    private readonly ILogger<Emailservice> _logger;

    public Emailservice(ILogger<Emailservice> logger)
    {
        _logger = logger;
    }

    public async Task SendWelcomeEmail(string email, string name)
    {
        _logger.LogInformation("Sending welcome email to employee (name={Name}, email={Email}).", name, email);

        await Task.Delay(2000); // simulate sending email
        _logger.LogInformation("Welcome email completed for employee (email={Email}).", email);
    }
}

using System;
using MyApi.Service.Interface;
namespace MyApi.Service.Implementation;

public class Emailservice: Iemailservice
{
    public async Task SendWelcomeEmail(string email, string name)
    {
        Console.WriteLine($"Sending welcome email to {name} at {email}");

        await Task.Delay(2000); // simulate sending email
    }
}

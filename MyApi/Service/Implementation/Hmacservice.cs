using System;
using Microsoft.Extensions.Logging;
using MyApi.Service.Interface;
using System.Security.Cryptography;
using System.Text;
namespace MyApi.Service.Implementation;

public class Hmacservice:IHmacservice
{
    private readonly ILogger<Hmacservice> _logger;

    public Hmacservice(ILogger<Hmacservice> logger)
    {
        _logger = logger;
    }

    public string GenerateSignature(string data, string secret)
    {
        _logger.LogInformation("Generating HMAC signature.");
        var key = Encoding.UTF8.GetBytes(secret);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

        return Convert.ToBase64String(hash);
    }

    public string GenerateHmacSecret()
    {
        _logger.LogInformation("Generating HMAC secret.");
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}

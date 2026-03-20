using System;
using MyApi.Service.Interface;
using System.Security.Cryptography;
using System.Text;
namespace MyApi.Service.Implementation;

public class Hmacservice:IHmacservice
{
    public string GenerateSignature(string data, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

        return Convert.ToBase64String(hash);
    }

    public string GenerateHmacSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}

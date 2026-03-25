using System;
using Microsoft.Extensions.Logging;
using MyApi.Service.Interface;
using System.Security.Cryptography;
using System.Text;
namespace MyApi.Service.Implementation;

public class Hmacservice:IHmacservice
{
    private readonly ILogger<Hmacservice> _logger;
    private readonly byte[] _key;
    public Hmacservice(ILogger<Hmacservice> logger,IConfiguration config)
    {
        _logger = logger;
        _key = Encoding.UTF8.GetBytes(config["Hmac:EncryptionKey"]!);
    }

    public (string apiKey, string secret) GenerateCredentials()
    {
        _logger.LogInformation("Generating new API credentials.");
        var apiKey = Guid.NewGuid().ToString();

        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToBase64String(secretBytes);

        return (apiKey, secret);
    }

    public string Encrypt(string plainText)
    {
        _logger.LogInformation("Encrypting data using HMAC service.");  
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        var iv = aes.IV;

        using var enc = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plainText);

        var cipher = enc.TransformFinalBlock(bytes, 0, bytes.Length);

        return Convert.ToBase64String(iv.Concat(cipher).ToArray());
    }

    public string Decrypt(string cipherText)
    {
        _logger.LogInformation("Decrypting data using HMAC service.");
        var full = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = full.Take(16).ToArray();
        var cipher = full.Skip(16).ToArray();

        aes.IV = iv;

        using var dec = aes.CreateDecryptor();
        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plain);
    }

    public string GenerateSignature(string secret, string data)
    {
        _logger.LogInformation("Generating HMAC signature.");
        var keyBytes = Convert.FromBase64String(secret);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

        return Convert.ToBase64String(hash);
    }

    public bool VerifySignature(string secret, string data, string providedSignature)
    {
        _logger.LogInformation("Verifying HMAC signature.");
        var computed = GenerateSignature(secret, data);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(providedSignature)
        );
    }
}

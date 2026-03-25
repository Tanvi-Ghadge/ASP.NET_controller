using System;

namespace MyApi.Service.Interface;

public interface IHmacservice
{
    (string apiKey, string secret) GenerateCredentials();

    string Encrypt(string plainText);
    string Decrypt(string cipherText);

    string GenerateSignature(string secret, string data);

    bool VerifySignature(string secret, string data, string providedSignature);
}

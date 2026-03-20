using System;

namespace MyApi.Service.Interface;

public interface IHmacservice
{
    string GenerateSignature(string data, string secret);

    string GenerateHmacSecret();
}

using System;
using MyApi.DTO.Employee;
using MyApi.models.entities;
namespace MyApi.Service.Interface;

public interface Iauthservice
{
    Task Register(Registerdto dto);

    Task<(string accessToken, string refreshToken)> Login(Logindto dto);

    Task<string?> RefreshToken(string token);
}

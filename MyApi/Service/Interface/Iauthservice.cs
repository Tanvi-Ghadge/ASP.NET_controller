using System;
using MyApi.DTO.Employee;
using MyApi.models.entities;
namespace MyApi.Service.Interface;

public interface Iauthservice
{
    Task<(string accessToken, string rawrefreshToken)> Register(Registerdto dto);

    Task<(string accessToken, string rawrefreshToken)> Login(Logindto dto);

    Task<(string accessToken, string rawrefreshToken)> RefreshToken(string token);
}

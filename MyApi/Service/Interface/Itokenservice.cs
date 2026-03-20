using System;
using MyApi.models.entities;
namespace MyApi.Service.Interface;

public interface Itokenservice
{
    string CreateAccessToken(Employee employee);
    string GenerateRefreshToken();
    string HashToken(string token);
    Refreshtoken CreateRefreshToken(Employee employee, string rawToken);
    
}

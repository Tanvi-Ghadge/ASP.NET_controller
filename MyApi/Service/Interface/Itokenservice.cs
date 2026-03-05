using System;
using MyApi.models.entities;
namespace MyApi.Service.Interface;

public interface Itokenservice
{
    string CreateAccessToken(Employee employee);

    Refreshtoken GenerateRefreshToken();
}

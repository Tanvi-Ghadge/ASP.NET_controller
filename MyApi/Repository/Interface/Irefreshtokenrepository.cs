using System;
using MyApi.models.entities;

namespace MyApi.Repository.Interface;

public interface Irefreshtokenrepository
{
    Task Add(Refreshtoken tokens);

    Task<Refreshtoken?> GetToken(string token);
}

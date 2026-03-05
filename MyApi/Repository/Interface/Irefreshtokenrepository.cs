using System;
using MyApi.models.entities;

namespace MyApi.Repository.Interface;

public interface Irefreshtokenrepository
{
    Task Add(Refreshtoken token);

    Task<Refreshtoken?> GetToken(string token);
}

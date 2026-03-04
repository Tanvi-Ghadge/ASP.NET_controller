using System;
using MyApi.models.entities;
namespace MyApi.Repository.Interface;

public interface Iemployeerepository
{
    Task<List<Employee>> GetAllAsync();

    Task<Employee?> GetByIdAsync(int id);

    Task AddAsync(Employee employee);

     void Update(Employee employee);

    void Delete(Employee employee);

    Task SaveAsync();
}

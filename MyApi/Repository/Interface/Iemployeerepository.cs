using System;
using MyApi.models.entities;
namespace MyApi.Repository.Interface;

public interface Iemployeerepository
{
    Task<List<Employee>> GetAllAsync();
    IQueryable<Employee> GetAll();

    Task<Employee?> GetByIdAsync(int id);
    Task<List<Employee>> GetByDeptIdAsync(int DeptId);
    Task<Employee?> GetByEmailAsync(string email);

    Task AddAsync(Employee employee);

     void Update(Employee employee);

    void Delete(Employee employee);

    Task SaveAsync();
}

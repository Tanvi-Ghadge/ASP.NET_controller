using System;
using MyApi.DTO.Employee;
namespace MyApi.Repository.Interface;

public interface IDapperrepo
{
    Task<List<Reademployeedto>> GetAllAsync();
    Task<IEnumerable<Reademployeedto>> GetEmployeesWithProjectsAsync();
    Task<Reademployeedto?> GetByIdAsync(int id);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<int> CreateAsync(Createemployeedto dto, string passwordHash, string hmacSecret);
    Task<bool> UpdateAsync(int id, Updateemployeedto dto);
    Task<bool> DeleteAsync(int id);
}

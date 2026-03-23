using System;
using MyApi.DTO.Employee;
namespace MyApi.Service.Interface;

public interface IDapperservice
{
    Task<List<Reademployeedto>> GetAllEmployeesAsync();
    Task<Reademployeedto?> GetEmployeeByIdAsync(int id);
    Task<Reademployeedto> CreateEmployeeAsync(Createemployeedto dto);
    Task<Reademployeedto?> UpdateEmployeeAsync(int id, Updateemployeedto dto);
    Task<bool> DeleteEmployeeAsync(int id);
}

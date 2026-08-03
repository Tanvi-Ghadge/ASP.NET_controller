using System;
using MyApi.DTO.Employee;

namespace MyApi.Service.Interface;

public interface Iemployeeservice
{
        Task<List<Reademployeedto>> GetAllEmployeesAsync();
        Task<object> GetEmployees(EmployeeQueryDto query);

    Task<Reademployeedto?> GetEmployeeByIdAsync(int id);
    

    Task<Reademployeedto> CreateEmployeeAsync(Createemployeedto dto);

    Task<Reademployeedto?> UpdateEmployeeAsync(int id, Updateemployeedto dto);

    Task<bool> DeleteEmployeeAsync(int id);

    /// <summary>
    /// Finds employees whose department name matches <paramref name="department"/> (case-insensitive contains).
    /// </summary>
    Task<List<Reademployeedto>> SearchEmployeesByDepartmentAsync(string department);

    /// <summary>
    /// Partially updates an employee, optionally resolving department by name.
    /// Reuses <see cref="UpdateEmployeeAsync"/> for persistence.
    /// </summary>
    Task<Reademployeedto?> UpdateEmployeeDetailsAsync(
        int id,
        string? name = null,
        string? email = null,
        decimal? salary = null,
        int? departmentId = null,
        string? departmentName = null,
        int? managerId = null);
}

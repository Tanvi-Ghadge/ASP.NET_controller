using MyApi.DTO.Employee;
using MyApi.models.entities;
using MyApi.Repository.Interface;
using MyApi.Service.Interface;
using AutoMapper;
using MyApi.Mappings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
namespace MyApi.Service.Implementation;
public class EmployeeService : Iemployeeservice
{
    private readonly Iemployeerepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private const string EmployeesCacheKey = "employees_all";
    public EmployeeService(Iemployeerepository repository, IMapper mapper, IMemoryCache cache)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
    }

public async Task<List<Reademployeedto>> GetAllEmployeesAsync()
{
    if (!_cache.TryGetValue(EmployeesCacheKey, out List<Reademployeedto>? employees))
    {
        var employeeEntities = await _repository.GetAllAsync();

        employees = employeeEntities.Select(e => new Reademployeedto
        {
            Id = e.Id,
            Name = e.Name,
            Email = e.Email,
            Salary = e.Salary,
            DepartmentName = e.Department!.Name,
            ManagerName = e.Manager?.Name
        }).ToList();

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

        _cache.Set(EmployeesCacheKey, employees, cacheOptions);
    }

    return employees ?? new List<Reademployeedto>();
}

    public async Task<Reademployeedto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _repository.GetByIdAsync(id);

        if (employee == null)
            return null;

        return new Reademployeedto
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Salary = employee.Salary,
            DepartmentName = employee.Department!.Name,
            ManagerName = employee.Manager?.Name
        };
    }

    public async Task<Reademployeedto> CreateEmployeeAsync(Createemployeedto dto)
    {
        var employee = new Employee
        {
            Name = dto.Name,
            Email = dto.Email,
            Salary = dto.Salary,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        await _repository.AddAsync(employee);
        _cache.Remove(EmployeesCacheKey);
        await _repository.SaveAsync();

        
        var created = await _repository.GetByIdAsync(employee.Id);

        return new Reademployeedto
        {
            Id = created!.Id,
            Name = created.Name,
            Email = created.Email,
            Salary = created.Salary,
            DepartmentName = created.Department!.Name,
            ManagerName = created.Manager?.Name
        };
    }

    public async Task<Reademployeedto?> UpdateEmployeeAsync(int id, Updateemployeedto dto)
    {
        var employee = await _repository.GetByIdAsync(id);

        if (employee == null)
            return null;

        _mapper.Map(dto, employee);

        _repository.Update(employee);
        _cache.Remove(EmployeesCacheKey);
        await _repository.SaveAsync();

        return _mapper.Map<Reademployeedto>(employee);
        
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var employee = await _repository.GetByIdAsync(id);

        if (employee == null)
            return false;

        _repository.Delete(employee);
        _cache.Remove(EmployeesCacheKey);
        await _repository.SaveAsync();

        return true;
    }
}
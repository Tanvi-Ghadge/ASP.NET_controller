using MyApi.DTO.Employee;
using MyApi.models.entities;
using MyApi.Repository.Interface;
using MyApi.Service.Interface;
using AutoMapper;
using MyApi.Mappings;
using Microsoft.AspNetCore.Http.HttpResults;
namespace MyApi.Service.Implementation;
public class EmployeeService : Iemployeeservice
{
    private readonly Iemployeerepository _repository;
    private readonly IMapper _mapper;
    public EmployeeService(Iemployeerepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<Reademployeedto>> GetAllEmployeesAsync()
    {
        var employees = await _repository.GetAllAsync();

        return employees.Select(e => new Reademployeedto
        {
            Id = e.Id,
            Name = e.Name,
            Email = e.Email,
            Salary = e.Salary,
            DepartmentName = e.Department!.Name,
            ManagerName = e.Manager?.Name
        }).ToList();
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
        await _repository.SaveAsync();

        return _mapper.Map<Reademployeedto>(employee);
        
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var employee = await _repository.GetByIdAsync(id);

        if (employee == null)
            return false;

        _repository.Delete(employee);
        await _repository.SaveAsync();

        return true;
    }
}
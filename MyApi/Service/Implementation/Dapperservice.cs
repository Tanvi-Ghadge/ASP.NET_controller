using System.Security.Cryptography;
using BCrypt.Net;
using Microsoft.Extensions.Logging;
using MyApi.DTO.Employee;
using MyApi.Repository.Interface;
using MyApi.Service.Interface;

namespace MyApi.Service.Implementation;

public class DapperEmployeeService : IDapperservice
{
    private readonly IDapperrepo _repository;
    private readonly ILogger<DapperEmployeeService> _logger;

    public DapperEmployeeService(IDapperrepo repository, ILogger<DapperEmployeeService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<List<Reademployeedto>> GetAllEmployeesAsync()
    {
        _logger.LogInformation("Fetching all employees through Dapper service.");
        return _repository.GetAllAsync();
    }

   

    public Task<Reademployeedto?> GetEmployeeByIdAsync(int id)
    {
        _logger.LogInformation("Fetching employee by id through Dapper service (id={EmployeeId}).", id);
        return _repository.GetByIdAsync(id);
    }

    public async Task<Reademployeedto> CreateEmployeeAsync(Createemployeedto dto)
    {
        _logger.LogInformation("Creating employee through Dapper service (email={Email}, name={Name}).", dto.Email, dto.Name);

        if (await _repository.EmailExistsAsync(dto.Email))
        {
            _logger.LogWarning("Employee create rejected because email already exists ({Email}).", dto.Email);
            throw new InvalidOperationException("Email already in use");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        var hmacSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var id = await _repository.CreateAsync(dto, passwordHash, hmacSecret);
        var created = await _repository.GetByIdAsync(id);

        if (created == null)
        {
            throw new InvalidOperationException("Employee was created but could not be reloaded");
        }

        return created;
    }

    public async Task<Reademployeedto?> UpdateEmployeeAsync(int id, Updateemployeedto dto)
    {
        _logger.LogInformation("Updating employee through Dapper service (id={EmployeeId}).", id);

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Employee update skipped because record was not found (id={EmployeeId}).", id);
            return null;
        }

        if (await _repository.EmailExistsAsync(dto.Email, id))
        {
            _logger.LogWarning("Employee update rejected because email already exists ({Email}).", dto.Email);
            throw new InvalidOperationException("Email already in use");
        }

        await _repository.UpdateAsync(id, dto);
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Reademployeedto>> GetEmployeesWithProjectsAsync()
    {
        _logger.LogInformation("Fetching employees with projects (many-to-many)");

        return await _repository.GetEmployeesWithProjectsAsync();
    }

    public Task<bool> DeleteEmployeeAsync(int id)
    {
        _logger.LogInformation("Deleting employee through Dapper service (id={EmployeeId}).", id);
        return _repository.DeleteAsync(id);
    }
}
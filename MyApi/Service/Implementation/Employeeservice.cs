using MyApi.DTO.Employee;
using MyApi.models.entities;
using MyApi.Repository.Interface;
using MyApi.Service.Interface;
using AutoMapper;
using MyApi.Mappings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Hangfire;
using Microsoft.EntityFrameworkCore;
namespace MyApi.Service.Implementation;
public class EmployeeService : Iemployeeservice
{
    private readonly Iemployeerepository _repository;
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EmployeeService> _logger;
    private const string EmployeesCacheKey = "employees_all";
    public EmployeeService(Iemployeerepository repository, IMapper mapper, IMemoryCache cache, ILogger<EmployeeService> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

public async Task<List<Reademployeedto>> GetAllEmployeesAsync()
{
    _logger.LogInformation("Fetching all employees from service.");
    if (!_cache.TryGetValue(EmployeesCacheKey, out List<Reademployeedto>? employees))
    {
        _logger.LogInformation("Employee list cache miss. Loading from repository.");
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
        _logger.LogInformation("Employee list cached with {EmployeeCount} records.", employees.Count);
    }
    else
    {
        _logger.LogInformation("Employee list cache hit.");
    }

    return employees ?? new List<Reademployeedto>();
}

    public async Task<object> GetEmployees(EmployeeQueryDto query)
    {
        _logger.LogInformation("Fetching employees with query (search={Search}, departmentId={DepartmentId}, minSalary={MinSalary}, page={Page}, pageSize={PageSize}, sortBy={SortBy}, desc={Desc}).",
            query.Search, query.DepartmentId, query.MinSalary, query.Page, query.PageSize, query.SortBy, query.Desc);
        var employees = _repository.GetAll();

        // SEARCH
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            employees = employees.Where(e =>
                e.Name.Contains(search) ||
                e.Email.Contains(search));
        }

        // FILTER
        if (query.DepartmentId.HasValue)
        {
            employees = employees.Where(e => e.DepartmentId == query.DepartmentId.Value);
        }

        if (query.MinSalary.HasValue)
        {
            employees = employees.Where(e => e.Salary >= query.MinSalary.Value);
        }

        // SORT
        employees = query.SortBy?.ToLower() switch
        {
            "salary" => query.Desc ? employees.OrderByDescending(e => e.Salary) : employees.OrderBy(e => e.Salary),
            "name" => query.Desc ? employees.OrderByDescending(e => e.Name) : employees.OrderBy(e => e.Name),
            _ => employees.OrderBy(e => e.Id)
        };

        var totalCount = await employees.CountAsync();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 1 : (query.PageSize > 100 ? 100 : query.PageSize);

        // PAGINATION + PROJECTION
        var data = await employees
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new Reademployeedto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                DepartmentName = e.Department != null ? e.Department.Name : string.Empty,
                Salary = e.Salary,
                ManagerName = e.Manager != null ? e.Manager.Name : null
            })
            .ToListAsync();

        _logger.LogInformation("Employee query completed with {ResultCount} results out of {TotalCount}.", data.Count, totalCount);

        return new
        {
            totalCount,
            page,
            pageSize,
            data
        };
    }
    
    public async Task<Reademployeedto?> GetEmployeeByIdAsync(int id)
    {
        _logger.LogInformation("Fetching employee by id from service (id={EmployeeId}).", id);
        var employee = await _repository.GetByIdAsync(id);

        if (employee == null)
        {
            _logger.LogWarning("Employee not found in service (id={EmployeeId}).", id);
            return null;
        }

        return _mapper.Map<Reademployeedto>(employee);
    }

    public async Task<Reademployeedto> CreateEmployeeAsync(Createemployeedto dto)
    {
        _logger.LogInformation("Creating employee in service (email={Email}, name={Name}).", dto.Email, dto.Name);
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
        _cache.Remove(EmployeesCacheKey);
        _logger.LogInformation("Employee created and cache invalidated (id={EmployeeId}).", employee.Id);
        BackgroundJob.Enqueue<Iemailservice>(
    x => x.SendWelcomeEmail(employee.Email, employee.Name));
        _logger.LogInformation("Welcome email job enqueued for employee (id={EmployeeId}).", employee.Id);
        
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
        _logger.LogInformation("Updating employee in service (id={EmployeeId}).", id);
        var employee = await _repository.GetByIdAsync(id);

        if (employee == null)
        {
            _logger.LogWarning("Update skipped because employee was not found (id={EmployeeId}).", id);
            return null;
        }

        _mapper.Map(dto, employee);

        _repository.Update(employee);
        
        await _repository.SaveAsync();
        _cache.Remove(EmployeesCacheKey);
        _logger.LogInformation("Employee updated and cache invalidated (id={EmployeeId}).", id);

        return _mapper.Map<Reademployeedto>(employee);
        
    }

    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        _logger.LogInformation("Deleting employee in service (id={EmployeeId}).", id);
        var employee = await _repository.GetByIdAsync(id);

        if (employee == null)
        {
            _logger.LogWarning("Delete skipped because employee was not found (id={EmployeeId}).", id);
            return false;
        }

        _repository.Delete(employee);
        
        await _repository.SaveAsync();
        _cache.Remove(EmployeesCacheKey);
        _logger.LogInformation("Employee deleted and cache invalidated (id={EmployeeId}).", id);

        return true;
    }
}

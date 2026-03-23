using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApi.data;
using MyApi.models.entities;
using MyApi.Repository.Interface;
namespace MyApi.Repository.Implementation;
public class EmployeeRepository : Iemployeerepository
{
    private readonly Dbcontext dbcontext;
    private readonly ILogger<EmployeeRepository> _logger;

    public EmployeeRepository(Dbcontext dbcontext, ILogger<EmployeeRepository> logger)
    {
        this.dbcontext = dbcontext;
        _logger = logger;
    }

    public async Task<List<Employee>> GetAllAsync()
    {
        _logger.LogInformation("Fetching all employees from repository.");
        return await dbcontext.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking()
            .ToListAsync();
    }

    public IQueryable<Employee> GetAll()
    {
        _logger.LogInformation("Building employee queryable in repository.");
        return dbcontext.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking();
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Fetching employee by id from repository (id={EmployeeId}).", id);
        return await dbcontext.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }
    public async Task<List<Employee>> GetByDeptIdAsync(int DeptId)
    {
        _logger.LogInformation("Fetching employees by department from repository (departmentId={DepartmentId}).", DeptId);
        return await dbcontext.Employees
            .Where(e => e.DepartmentId == DeptId)
            .Include(e => e.Department)
            .AsNoTracking()
            .ToListAsync();
    }
    public async Task<Employee?> GetByEmailAsync(string email)
    {
        _logger.LogInformation("Fetching employee by email from repository (email={Email}).", email);
        return await dbcontext.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task AddAsync(Employee employee)
    {
        _logger.LogInformation("Adding employee to repository (email={Email}, name={Name}).", employee.Email, employee.Name);
        await dbcontext.Employees.AddAsync(employee);
    }

    public void Update(Employee employee)
    {
        _logger.LogInformation("Updating employee in repository (id={EmployeeId}).", employee.Id);
        dbcontext.Employees.Update(employee);
    }

    public void Delete(Employee employee)
    {
        _logger.LogInformation("Deleting employee from repository (id={EmployeeId}).", employee.Id);
        dbcontext.Employees.Remove(employee);
    }

    public async Task SaveAsync()
    {
        _logger.LogInformation("Saving employee repository changes.");
        await dbcontext.SaveChangesAsync();
    }
}

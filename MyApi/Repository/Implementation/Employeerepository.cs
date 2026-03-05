using Microsoft.EntityFrameworkCore;
using MyApi.data;
using MyApi.models.entities;
using MyApi.Repository.Interface;
namespace MyApi.Repository.Implementation;
public class EmployeeRepository : Iemployeerepository
{
    private readonly Dbcontext dbcontext;

    public EmployeeRepository(Dbcontext dbcontext)
    {
        this.dbcontext = dbcontext;
    }

    public async Task<List<Employee>> GetAllAsync()
    {
        return await dbcontext.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await dbcontext.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        return await dbcontext.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == email);
    }

    public async Task AddAsync(Employee employee)
    {
        await dbcontext.Employees.AddAsync(employee);
    }

    public void Update(Employee employee)
    {
        dbcontext.Employees.Update(employee);
    }

    public void Delete(Employee employee)
    {
        dbcontext.Employees.Remove(employee);
    }

    public async Task SaveAsync()
    {
        await dbcontext.SaveChangesAsync();
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApi.data;
using MyApi.models;
using MyApi.models.entities;
namespace MyApi.Controllers
{
    // localhost:xxxx/api/employees
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ControllerBase
    {
        private readonly Dbcontext dbContext;

        public EmployeesController(Dbcontext dbContext)
        {
            this.dbContext = dbContext;
        }
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await dbContext.Employees.Select(e => new Reademployeedto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Phone = e.Phone,
                Department = e.Department
            }).AsNoTracking().ToListAsync();
            return Ok(employees);
        }

        [HttpGet]
        [Route("{department}")]
        public async Task<IActionResult> GetEmployeesByDepartment(string department)
        {
            var employees = await dbContext.Employees.Where(e => e.Department == department).Select(e => new Reademployeedto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Phone = e.Phone,
                Department = e.Department
            }).AsNoTracking().ToListAsync();
            return Ok(employees);
        }

        [HttpGet]
        [Route("search/{name}/{department}")]
        public async Task<IActionResult> SearchEmployeesByName(string name,string department)
        {
            var query=  dbContext.Employees.AsQueryable();
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(e => e.Name.Contains(name));
            }
            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(e => e.Department == department);
            }
            var employees = await query.Select(e => new Reademployeedto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Phone = e.Phone,
                Department = e.Department
            }).AsNoTracking().ToListAsync();
            return Ok(employees);
        }

        [HttpGet]
        [Route("{id:guid}")]
        public async Task<IActionResult> GetEmployeebyID(Guid id)
        {
            var employee = await dbContext.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return Ok(new Reademployeedto
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Phone = employee.Phone,
                Department = employee.Department
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddEmployee(Addemployeedto addemployeedto)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                Name = addemployeedto.Name,
                Email = addemployeedto.Email,
                Phone = addemployeedto.Phone,
                Department = addemployeedto.Department
            };
            await dbContext.Employees.AddAsync(employee);
            await dbContext.SaveChangesAsync();
            var readEmployee = new Reademployeedto
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Phone = employee.Phone,
                Department = employee.Department
            };
            return CreatedAtAction(nameof(GetEmployeebyID), new { id = employee.Id }, readEmployee);
    }
    [HttpPut]
    [Route("{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, Updateemployeedto updateemployeedto)
    {
        var employee = await dbContext.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }
        employee.Name = updateemployeedto.Name;
        employee.Email = updateemployeedto.Email;
        employee.Phone = updateemployeedto.Phone;
        employee.Department = updateemployeedto.Department;
        await dbContext.SaveChangesAsync();
        var readEmployee = new Reademployeedto
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Phone = employee.Phone,
            Department = employee.Department
        };
        return Ok(readEmployee);
    }
    [HttpDelete]
    [Route("{id:guid}")]
    public async Task<IActionResult> DeleteEmployee(Guid id)
    {
        var employee = await dbContext.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound();
        }
        dbContext.Employees.Remove(employee);
        await dbContext.SaveChangesAsync();
        return Ok(new { message = "Employee deleted successfully." });
    }

    [HttpPatch]
    [Route("{id:guid}")]
    public async Task<IActionResult> PatchEmployee(Guid id, Updateemployeedto updateemployeedto)
    {
        var employee = await dbContext.Employees.FindAsync(id);
        if (employee == null)
        {
            return NotFound(new { message = "Employee not found." });
        }
        if (updateemployeedto.Name != null)
        {
            employee.Name = updateemployeedto.Name;
        }
        if (updateemployeedto.Email != null)
        {
            employee.Email = updateemployeedto.Email;
        }
        if (updateemployeedto.Phone != null)
        {
            employee.Phone = updateemployeedto.Phone;
        }
        if (updateemployeedto.Department != null)
        {
            employee.Department = updateemployeedto.Department;
        }
        await dbContext.SaveChangesAsync();
        var readEmployee = new Reademployeedto
        {
            Id = employee.Id,
            Name = employee.Name,
            Email = employee.Email,
            Phone = employee.Phone,
            Department = employee.Department
        };
        return Ok(readEmployee);
    }

    }
}
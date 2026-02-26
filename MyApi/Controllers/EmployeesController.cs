using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public IActionResult GetEmployees()
        {
            var employees = dbContext.Employees.Select(e => new Reademployeedto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                Phone = e.Phone,
                Department = e.Department
            }).ToList();
            return Ok(employees);
        }
        [HttpGet]
        [Route("{id:guid}")]
        public IActionResult GetEmployeebyID(Guid id)
        {
            var employee = dbContext.Employees.Find(id);
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
        public IActionResult AddEmployee(Addemployeedto addemployeedto)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                Name = addemployeedto.Name,
                Email = addemployeedto.Email,
                Phone = addemployeedto.Phone,
                Department = addemployeedto.Department
            };
            dbContext.Employees.Add(employee);
            dbContext.SaveChanges();
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
    [HttpPut]
    [Route("{id:guid}")]
    public IActionResult UpdateEmployee(Guid id, Updateemployeedto updateemployeedto)
    {
        var employee = dbContext.Employees.Find(id);
        if (employee == null)
        {
            return NotFound();
        }
        employee.Name = updateemployeedto.Name;
        employee.Email = updateemployeedto.Email;
        employee.Phone = updateemployeedto.Phone;
        employee.Department = updateemployeedto.Department;
        dbContext.SaveChanges();
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
    public IActionResult DeleteEmployee(Guid id)
    {
        var employee = dbContext.Employees.Find(id);
        if (employee == null)
        {
            return NotFound();
        }
        dbContext.Employees.Remove(employee);
        dbContext.SaveChanges();
        return Ok(new { message = "Employee deleted successfully." });
    }

    [HttpPatch]
    [Route("{id:guid}")]
    public IActionResult PatchEmployee(Guid id, Updateemployeedto updateemployeedto)
    {
        var employee = dbContext.Employees.Find(id);
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
        dbContext.SaveChanges();
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
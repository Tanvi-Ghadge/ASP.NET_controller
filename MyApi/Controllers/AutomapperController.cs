using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyApi.data;
using MyApi.Mappings;
using MyApi.models;
using Microsoft.EntityFrameworkCore;
using MyApi.models.entities;
using AutoMapper.QueryableExtensions;
namespace MyApi.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class AutomapperController : ControllerBase
    {
        private readonly Dbcontext dbContext;
        private readonly IMapper _mapper;

        public AutomapperController(Dbcontext dbContext, IMapper _mapper)
        {
            this.dbContext = dbContext;
            this._mapper = _mapper;
        }
        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var employees = await dbContext.Employees.ProjectTo<Reademployeedto>(_mapper.ConfigurationProvider).ToListAsync();  //ProjectTo is an extension method provided by AutoMapper that allows you to project a queryable collection of entities (in this case, Employees) directly into a collection of DTOs (Reademployeedto) using the mapping configuration defined in AutoMapper.
            // var employeeDtos = _mapper.Map<List<Reademployeedto>>(employees);  //MappingOperationOptions the complete list of employees to a list of Reademployeedto using AutoMapper
            return Ok(employees);
        }

        [HttpGet]
        [Route("{pageNumber:int}/{pageSize:int}")]
        public async Task<IActionResult> PaginationGetemployees(int pageNumber, int pageSize)
        {
            var employees =await dbContext.Employees.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var employeeDtos = _mapper.Map<List<Reademployeedto>>(employees);
            return Ok(employeeDtos);
        }

        [HttpGet]
        [Route("{id:guid}")]
        public async Task<IActionResult> GetEmployeeById(Guid id)
        {
            var employee = await dbContext.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            var employeeDto = _mapper.Map<Reademployeedto>(employee);
            return Ok(employeeDto);
        }
        [HttpPost]
        public async Task<IActionResult> CreateEmployee(Addemployeedto createEmployeeDto)
        {
            var employee = _mapper.Map<Employee>(createEmployeeDto);
            await dbContext.Employees.AddAsync(employee);
            await dbContext.SaveChangesAsync();
            var employeeDto = _mapper.Map<Reademployeedto>(employee);
            return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id }, employeeDto);
        }
        
        [HttpPut]
        [Route("{id:guid}")]
        public async Task<IActionResult> UpdateEmployee(Guid id, Updateemployeedto updateEmployeeDto)
        {
            var employee = await dbContext.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            _mapper.Map(updateEmployeeDto, employee);
            await dbContext.SaveChangesAsync();
            var employeeDto = _mapper.Map<Reademployeedto>(employee);
            return Ok(employeeDto);
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
            return Ok("Employee deleted successfully.");
        }
    }
}

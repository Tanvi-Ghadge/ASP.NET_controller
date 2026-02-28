using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyApi.data;
using MyApi.Mappings;
using MyApi.models;
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
        public IActionResult GetEmployees()
        {
            var employees = dbContext.Employees.ToList();
            var employeeDtos = _mapper.Map<List<Reademployeedto>>(employees);  //MappingOperationOptions the complete list of employees to a list of Reademployeedto using AutoMapper
            return Ok(employeeDtos);
        }
        [HttpGet]
        [Route("{id:guid}")]
        public IActionResult GetEmployeeById(Guid id)
        {
            var employee = dbContext.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }
            var employeeDto = _mapper.Map<Reademployeedto>(employee);
            return Ok(employeeDto);
        }
        [HttpPost]
        public IActionResult CreateEmployee(Addemployeedto createEmployeeDto)
        {
            var employee = _mapper.Map<Employee>(createEmployeeDto);
            dbContext.Employees.Add(employee);
            dbContext.SaveChanges();
            var employeeDto = _mapper.Map<Reademployeedto>(employee);
            return CreatedAtAction(nameof(GetEmployeeById), new { id = employee.Id }, employeeDto);
        }
        
        [HttpPut]
        [Route("{id:guid}")]
        public IActionResult UpdateEmployee(Guid id, Updateemployeedto updateEmployeeDto)
        {
            var employee = dbContext.Employees.Find(id);
            if (employee == null)
            {
                return NotFound();
            }
            _mapper.Map(updateEmployeeDto, employee);
            dbContext.SaveChanges();
            var employeeDto = _mapper.Map<Reademployeedto>(employee);
            return Ok(employeeDto);
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
            return Ok("Employee deleted successfully.");
        }
    }
}

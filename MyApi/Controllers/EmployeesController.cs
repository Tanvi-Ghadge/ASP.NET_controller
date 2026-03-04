using Microsoft.AspNetCore.Mvc;
using MyApi.DTO.Employee;
using MyApi.Service.Interface;
[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly Iemployeeservice _service;

    public EmployeesController(Iemployeeservice service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _service.GetAllEmployeesAsync();
        return Ok(employees);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(int id)
    {
        var employee = await _service.GetEmployeeByIdAsync(id);

        if (employee == null)
            return NotFound();

        return Ok(employee);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployee(Createemployeedto dto)
    {
        var employee = await _service.CreateEmployeeAsync(dto);

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, Updateemployeedto dto)
    {
        var result = await _service.UpdateEmployeeAsync(id, dto);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var result = await _service.DeleteEmployeeAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }
}
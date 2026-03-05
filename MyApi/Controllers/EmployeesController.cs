using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MyApi.DTO.Employee;
using MyApi.Service.Interface;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class EmployeesController : ControllerBase
{
    private readonly Iemployeeservice _service;

    public EmployeesController(Iemployeeservice service)
    {
        _service = service;
    }

    // Any authenticated user can view employees
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

    // Only Admin can create employees
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(Createemployeedto dto)
    {
        var employee = await _service.CreateEmployeeAsync(dto);

        return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
    }

    // Only Admin can update
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, Updateemployeedto dto)
    {
        var result = await _service.UpdateEmployeeAsync(id, dto);

        if (result == null)
            return NotFound();

        return Ok(result);
    }

    // Only Admin can delete
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var result = await _service.DeleteEmployeeAsync(id);

        if (!result)
            return NotFound();

        return NoContent();
    }

    // Example endpoint to get logged-in user info
    [HttpGet("me")]
    public IActionResult GetLoggedInUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Role = role
        });
    }
}
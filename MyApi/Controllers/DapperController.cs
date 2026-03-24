using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.DTO.Employee;
using MyApi.Repository.Implementation;
using MyApi.Service.Implementation;
using MyApi.Service.Interface;

[ApiController]
[Route("api/dapper-employees")]
[Authorize]
public class DapperEmployeesController : ControllerBase
{
    private readonly IDapperservice _service;
    private readonly ILogger<DapperEmployeesController> _logger;

    public DapperEmployeesController(
        IDapperservice service,
        ILogger<DapperEmployeesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("with-projects")]
    public async Task<IActionResult> GetEmployeesWithProjects()
    {
        var data = await _service.GetEmployeesWithProjectsAsync();
        return Ok(data);
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllEmployees()
    {
        try
        {
            _logger.LogInformation("Dapper GetAllEmployees called.");
            var result = await _service.GetAllEmployeesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper GetAllEmployees failed.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetEmployee(int id)
    {
        try
        {
            _logger.LogInformation("Dapper GetEmployee called (id={Id}).", id);
            var employee = await _service.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper GetEmployee failed (id={Id}).", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(Createemployeedto dto)
    {
        try
        {
            _logger.LogInformation("Dapper CreateEmployee called (email={Email}).", dto.Email);
            var employee = await _service.CreateEmployeeAsync(dto);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Dapper CreateEmployee validation failed.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper CreateEmployee failed.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateEmployee(int id, Updateemployeedto dto)
    {
        try
        {
            _logger.LogInformation("Dapper UpdateEmployee called (id={Id}).", id);
            var employee = await _service.UpdateEmployeeAsync(id, dto);

            if (employee == null)
            {
                return NotFound();
            }

            return Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Dapper UpdateEmployee validation failed (id={Id}).", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper UpdateEmployee failed (id={Id}).", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [Authorize(Roles = "Admin")]
    [RequireHmac]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        try
        {
            _logger.LogInformation("Dapper DeleteEmployee called (id={Id}).", id);
            var deleted = await _service.DeleteEmployeeAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dapper DeleteEmployee failed (id={Id}).", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
}
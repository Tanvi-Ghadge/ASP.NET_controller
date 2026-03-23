using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MyApi.DTO.Employee;
using MyApi.Service.Interface;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
[Authorize] // All endpoints require authentication
public class EmployeesController : ControllerBase
{
    private readonly Iemployeeservice _service;
    private readonly ILogger<EmployeesController> _logger;
    public EmployeesController(Iemployeeservice service, ILogger<EmployeesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // Any authenticated user can view employees
    [HttpGet]
    public async Task<IActionResult> GetEmployees([FromQuery] EmployeeQueryDto query)
    {
        try
        {
            _logger.LogInformation("GetEmployees called (search={Search}, departmentId={DepartmentId}, minSalary={MinSalary}, page={Page}, pageSize={PageSize}, sortBy={SortBy}, desc={Desc}).",
                query?.Search, query?.DepartmentId, query?.MinSalary, query?.Page, query?.PageSize, query?.SortBy, query?.Desc);

            var result = await _service.GetEmployees(query ?? new EmployeeQueryDto());
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetEmployees failed.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllEmployees()
    {
        try
        {
            _logger.LogInformation("GetAllEmployees called.");
            var result = await _service.GetAllEmployeesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAllEmployees failed.");
            return StatusCode(500, "An unexpected error occurred.");
        }
    }
    
    // Any authenticated user can view employees by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEmployee(int id)
    {
        try
        {
            _logger.LogInformation("GetEmployee called (id={Id}).", id);
            var employee = await _service.GetEmployeeByIdAsync(id);

            if (employee == null)
            {
                _logger.LogWarning("GetEmployee not found (id={Id}).", id);
                return NotFound();
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetEmployee failed (id={Id}).", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // Only Admin can create employees
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(Createemployeedto dto)
    {
        try
        {
            if (dto == null)
            {
                _logger.LogWarning("CreateEmployee called with null dto.");
                return BadRequest("Employee data is required.");
            }

            _logger.LogInformation("CreateEmployee called (email={Email}, name={Name}).", dto?.Email, dto?.Name);
            var employee = await _service.CreateEmployeeAsync(dto!);
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateEmployee failed (email={Email}, name={Name}).", dto?.Email, dto?.Name);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // Only Admin can update
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEmployee(int id, Updateemployeedto dto)
    {
        try
        {
            _logger.LogInformation("UpdateEmployee called (id={Id}, email={Email}).", id, dto?.Email);
            var result = await _service.UpdateEmployeeAsync(id, dto!);

            if (result == null)
            {
                _logger.LogWarning("UpdateEmployee not found (id={Id}).", id);
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateEmployee failed (id={Id}).", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // Only Admin can delete
    [Authorize(Roles = "Admin")]
    [RequireHmac]    //for HMAC protection on delete endpoint
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        try
        {
            _logger.LogInformation("DeleteEmployee called (id={Id}).", id);
            var result = await _service.DeleteEmployeeAsync(id);

            if (!result)
            {
                _logger.LogWarning("DeleteEmployee not found (id={Id}).", id);
                return NotFound();
            }

            _logger.LogInformation("DeleteEmployee succeeded (id={Id}).", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteEmployee failed (id={Id}).", id);
            return StatusCode(500, "An unexpected error occurred.");
        }
    }

    // Example endpoint to get logged-in user info
    [HttpGet("me")]
    public IActionResult GetLoggedInUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        _logger.LogInformation("GetLoggedInUser called (userId={UserId}, email={Email}, role={Role}).", userId, email, role);

        return Ok(new
        {
            UserId = userId,
            Email = email,
            Role = role
        });
    }
}
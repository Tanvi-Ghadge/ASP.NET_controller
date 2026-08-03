using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using MyApi.AI.Observability;
using MyApi.DTO.Employee;
using MyApi.Service.Interface;

namespace MyApi.AI.Plugins;

/// <summary>
/// AI tool surface for employee operations.
/// All tool calls pass through <see cref="IToolInvocationPipeline"/> (BeforeTool / AfterTool middleware).
/// </summary>
public sealed class EmployeePlugin
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly Iemployeeservice _employeeService;
    private readonly IToolInvocationPipeline _toolPipeline;
    private readonly ILogger<EmployeePlugin> _logger;

    public EmployeePlugin(
        Iemployeeservice employeeService,
        IToolInvocationPipeline toolPipeline,
        ILogger<EmployeePlugin> logger)
    {
        _employeeService = employeeService;
        _toolPipeline = toolPipeline;
        _logger = logger;
    }

    public IList<AITool> GetAITools() =>
    [
        AIFunctionFactory.Create(GetEmployeeById),
        AIFunctionFactory.Create(GetAllEmployees),
        AIFunctionFactory.Create(CreateEmployee),
        AIFunctionFactory.Create(UpdateEmployee),
        AIFunctionFactory.Create(DeleteEmployee),
        AIFunctionFactory.Create(SearchEmployeesByDepartment)
    ];

    [Description("Gets an employee by their unique numeric id. Use when the user asks to show, find, or look up a specific employee.")]
    public Task<string> GetEmployeeById([Description("The employee id to retrieve.")] int id) =>
        _toolPipeline.InvokeAsync(
            "EmployeePlugin",
            nameof(GetEmployeeById),
            new { id },
            async () =>
            {
                _logger.LogInformation("Tool GetEmployeeById invoked (id={EmployeeId}).", id);
                var employee = await _employeeService.GetEmployeeByIdAsync(id);
                return employee is null ? $"No employee found with id {id}." : Serialize(employee);
            });

    [Description("Lists all employees in the system. Use when the user asks to list, show all, or enumerate employees.")]
    public Task<string> GetAllEmployees() =>
        _toolPipeline.InvokeAsync(
            "EmployeePlugin",
            nameof(GetAllEmployees),
            null,
            async () =>
            {
                _logger.LogInformation("Tool GetAllEmployees invoked.");
                return Serialize(await _employeeService.GetAllEmployeesAsync());
            });

    [Description("Creates a new employee. Requires name, email, password, salary, and departmentId.")]
    public Task<string> CreateEmployee(
        [Description("Full name of the employee.")] string name,
        [Description("Email address of the employee.")] string email,
        [Description("Initial password for the employee account.")] string password,
        [Description("Salary amount.")] decimal salary,
        [Description("Numeric department id.")] int departmentId,
        [Description("Optional manager employee id.")] int? managerId = null) =>
        _toolPipeline.InvokeAsync(
            "EmployeePlugin",
            nameof(CreateEmployee),
            new { name, email, salary, departmentId, managerId },
            async () =>
            {
                _logger.LogInformation("Tool CreateEmployee invoked (email={Email}).", email);
                var created = await _employeeService.CreateEmployeeAsync(new Createemployeedto
                {
                    Name = name,
                    Email = email,
                    Password = password,
                    Salary = salary,
                    DepartmentId = departmentId,
                    ManagerId = managerId
                });
                return Serialize(created);
            });

    [Description("Updates an existing employee. Provide the employee id and any fields to change. Use departmentName when the user names a department such as HR or Finance.")]
    public Task<string> UpdateEmployee(
        [Description("The employee id to update.")] int id,
        [Description("Optional new name.")] string? name = null,
        [Description("Optional new email.")] string? email = null,
        [Description("Optional new salary.")] decimal? salary = null,
        [Description("Optional numeric department id.")] int? departmentId = null,
        [Description("Optional department name such as HR or Finance.")] string? departmentName = null,
        [Description("Optional manager employee id.")] int? managerId = null) =>
        _toolPipeline.InvokeAsync(
            "EmployeePlugin",
            nameof(UpdateEmployee),
            new { id, name, email, salary, departmentId, departmentName, managerId },
            async () =>
            {
                _logger.LogInformation(
                    "Tool UpdateEmployee invoked (id={EmployeeId}, departmentName={DepartmentName}).",
                    id,
                    departmentName);
                var updated = await _employeeService.UpdateEmployeeDetailsAsync(
                    id, name, email, salary, departmentId, departmentName, managerId);
                return updated is null
                    ? $"Unable to update employee {id}. The employee or department may not exist."
                    : Serialize(updated);
            });

    [Description("Deletes an employee by id. Use when the user asks to delete or remove an employee.")]
    public Task<string> DeleteEmployee([Description("The employee id to delete.")] int id) =>
        _toolPipeline.InvokeAsync(
            "EmployeePlugin",
            nameof(DeleteEmployee),
            new { id },
            async () =>
            {
                _logger.LogInformation("Tool DeleteEmployee invoked (id={EmployeeId}).", id);
                var deleted = await _employeeService.DeleteEmployeeAsync(id);
                return deleted
                    ? $"Employee {id} was deleted successfully."
                    : $"No employee found with id {id}; nothing was deleted.";
            });

    [Description("Finds employees who work in a department by department name (for example Finance, HR). Use when the user asks who works in a department.")]
    public Task<string> SearchEmployeesByDepartment(
        [Description("Department name to search for, such as Finance or HR.")] string department) =>
        _toolPipeline.InvokeAsync(
            "EmployeePlugin",
            nameof(SearchEmployeesByDepartment),
            new { department },
            async () =>
            {
                _logger.LogInformation("Tool SearchEmployeesByDepartment invoked (department={Department}).", department);
                var employees = await _employeeService.SearchEmployeesByDepartmentAsync(department);
                return employees.Count == 0
                    ? $"No employees found in department matching '{department}'."
                    : Serialize(employees);
            });

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, SerializerOptions);
}

using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using MyApi.DTO.Employee;
using MyApi.Service.Interface;

namespace MyApi.AI.Plugins;

/// <summary>
/// Payroll-domain tools. Reuses <see cref="Iemployeeservice"/> for salary data — no separate payroll repository.
/// </summary>
public sealed class PayrollPlugin
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Iemployeeservice _employeeService;
    private readonly ILogger<PayrollPlugin> _logger;

    public PayrollPlugin(Iemployeeservice employeeService, ILogger<PayrollPlugin> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    public IList<AITool> GetAITools() =>
    [
        AIFunctionFactory.Create(GetPayrollByEmployeeId),
        AIFunctionFactory.Create(GetCompensationSummary)
    ];

    [Description("Gets payroll/compensation details for an employee by id.")]
    public async Task<string> GetPayrollByEmployeeId(
        [Description("Employee id.")] int employeeId)
    {
        _logger.LogInformation("PayrollPlugin.GetPayrollByEmployeeId({EmployeeId})", employeeId);
        var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
        if (employee is null)
        {
            return $"No payroll found for employee {employeeId}.";
        }

        var payroll = BuildPayroll(employee);
        return JsonSerializer.Serialize(payroll, SerializerOptions);
    }

    [Description("Gets a compensation summary for all employees.")]
    public async Task<string> GetCompensationSummary()
    {
        _logger.LogInformation("PayrollPlugin.GetCompensationSummary()");
        var employees = await _employeeService.GetAllEmployeesAsync();
        var summary = new
        {
            employeeCount = employees.Count,
            totalPayroll = employees.Sum(e => e.Salary),
            averageSalary = employees.Count == 0 ? 0 : employees.Average(e => e.Salary),
            minSalary = employees.Count == 0 ? 0 : employees.Min(e => e.Salary),
            maxSalary = employees.Count == 0 ? 0 : employees.Max(e => e.Salary)
        };
        return JsonSerializer.Serialize(summary, SerializerOptions);
    }

    /// <summary>Typed helper for A2A handlers.</summary>
    public async Task<object?> GetPayrollRecordAsync(int employeeId)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
        return employee is null ? null : BuildPayroll(employee);
    }

    public async Task<object> GetCompensationSummaryAsync()
    {
        var employees = await _employeeService.GetAllEmployeesAsync();
        return new
        {
            EmployeeCount = employees.Count,
            TotalPayroll = employees.Sum(e => e.Salary),
            AverageSalary = employees.Count == 0 ? 0 : employees.Average(e => e.Salary),
            MinSalary = employees.Count == 0 ? 0 : employees.Min(e => e.Salary),
            MaxSalary = employees.Count == 0 ? 0 : employees.Max(e => e.Salary),
            Employees = employees.Select(e => BuildPayroll(e)).ToList()
        };
    }

    private static object BuildPayroll(Reademployeedto employee) => new
    {
        employee.Id,
        employee.Name,
        employee.Email,
        employee.DepartmentName,
        BaseSalary = employee.Salary,
        EstimatedMonthly = Math.Round(employee.Salary / 12m, 2),
        EstimatedBonus = Math.Round(employee.Salary * 0.1m, 2),
        Currency = "USD"
    };
}

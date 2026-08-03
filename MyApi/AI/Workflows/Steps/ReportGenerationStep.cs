using System.Text;
using System.Text.Json;
using MyApi.AI.Memory;
using MyApi.AI.Plugins;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;
using MyApi.DTO.Employee;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Loads employees through <see cref="EmployeePlugin"/>, calculates statistics, and generates a text report.
/// </summary>
public sealed class ReportGenerationStep : IWorkflowStep
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly EmployeePlugin _employeePlugin;
    private readonly ILogger<ReportGenerationStep> _logger;

    public ReportGenerationStep(
        EmployeePlugin employeePlugin,
        ILogger<ReportGenerationStep> logger)
    {
        _employeePlugin = employeePlugin;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "GenerateReport";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 1) Load employees
        var payload = await _employeePlugin.GetAllEmployees();
        var employees = JsonSerializer.Deserialize<List<Reademployeedto>>(payload, JsonOptions)
            ?? new List<Reademployeedto>();
        context.State.Set("Employees", employees);
        _logger.LogInformation(
            "Loaded {Count} employees for report via EmployeePlugin. CorrelationId={CorrelationId}",
            employees.Count,
            context.CorrelationId);

        // 2) Calculate statistics
        var stats = CalculateStatistics(employees);
        context.State.Set("ReportStatistics", stats);

        // 3) Generate summary + 4) Generate report (text; PDF later)
        var preferredFormat = ResolvePreferredFormat(context.Memory);
        var report = BuildReport(employees, stats, preferredFormat);
        context.State.Set("ReportText", report);
        context.State.Set("FinalResponse", report);

        _logger.LogInformation(
            "Generated text employee report (preferredFormat={Format}). CorrelationId={CorrelationId}",
            preferredFormat,
            context.CorrelationId);

        return StepResult.Success("Report generated.", report);
    }

    private static ReportStatistics CalculateStatistics(IReadOnlyList<Reademployeedto> employees)
    {
        if (employees.Count == 0)
        {
            return new ReportStatistics(0, 0, 0, 0, Array.Empty<string>());
        }

        var salaries = employees.Select(e => e.Salary).ToList();
        var departments = employees
            .GroupBy(e => string.IsNullOrWhiteSpace(e.DepartmentName) ? "(unknown)" : e.DepartmentName)
            .OrderByDescending(g => g.Count())
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();

        return new ReportStatistics(
            employees.Count,
            salaries.Average(),
            salaries.Min(),
            salaries.Max(),
            departments);
    }

    private static string ResolvePreferredFormat(MemoryContext memory)
    {
        var preferred = memory.Memories
            .FirstOrDefault(m =>
                m.Key.Equals("PreferredReportFormat", StringComparison.OrdinalIgnoreCase));

        return preferred?.Value ?? "Text";
    }

    private static string BuildReport(
        IReadOnlyList<Reademployeedto> employees,
        ReportStatistics stats,
        string preferredFormat)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Employee Report");
        sb.AppendLine($"Format preference: {preferredFormat} (text body for now; PDF/Excel adapters can be plugged in later).");
        sb.AppendLine(new string('-', 48));
        sb.AppendLine($"Total employees: {stats.Count}");
        sb.AppendLine($"Average salary: {stats.AverageSalary:C}");
        sb.AppendLine($"Min salary: {stats.MinSalary:C}");
        sb.AppendLine($"Max salary: {stats.MaxSalary:C}");
        sb.AppendLine();
        sb.AppendLine("Headcount by department:");
        foreach (var line in stats.DepartmentBreakdown)
        {
            sb.AppendLine($"  - {line}");
        }

        sb.AppendLine();
        sb.AppendLine("Employees:");
        foreach (var employee in employees.OrderBy(e => e.Id))
        {
            sb.AppendLine(
                $"  #{employee.Id} {employee.Name} | {employee.DepartmentName} | {employee.Salary:C}");
        }

        if (preferredFormat.Equals("PDF", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine();
            sb.AppendLine("Note: PreferredReportFormat is PDF. Returning a text report now; PDF rendering can replace this step later.");
        }

        return sb.ToString().TrimEnd();
    }

    private sealed record ReportStatistics(
        int Count,
        decimal AverageSalary,
        decimal MinSalary,
        decimal MaxSalary,
        IReadOnlyList<string> DepartmentBreakdown);
}

using System.Text.Json;
using System.Text.RegularExpressions;
using MyApi.AI.Plugins;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;
using MyApi.DTO.Employee;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Loads a single employee by id parsed from the user message via <see cref="EmployeePlugin"/>.
/// </summary>
public sealed partial class EmployeeLookupStep : IWorkflowStep
{
    public const string EmployeeVariableKey = "Employee";
    public const string EmployeeIdVariableKey = "EmployeeId";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly EmployeePlugin _employeePlugin;
    private readonly ILogger<EmployeeLookupStep> _logger;

    public EmployeeLookupStep(
        EmployeePlugin employeePlugin,
        ILogger<EmployeeLookupStep> logger)
    {
        _employeePlugin = employeePlugin;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "LoadEmployee";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var message = context.Harness.CurrentMessage;
        var match = EmployeeIdRegex().Match(message);
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var employeeId))
        {
            _logger.LogInformation(
                "No employee id found in message; skipping lookup. CorrelationId={CorrelationId}",
                context.CorrelationId);
            return StepResult.Skip("No employee id present in the user message.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var payload = await _employeePlugin.GetEmployeeById(employeeId);

        if (payload.StartsWith("No employee found", StringComparison.OrdinalIgnoreCase))
        {
            context.State.Set("FinalResponse", payload);
            return StepResult.Success(payload);
        }

        var employee = JsonSerializer.Deserialize<Reademployeedto>(payload, JsonOptions);
        if (employee is null)
        {
            return StepResult.Failure("Failed to deserialize employee payload.");
        }

        context.State.Set(EmployeeIdVariableKey, employeeId);
        context.State.Set(EmployeeVariableKey, employee);

        _logger.LogInformation(
            "Loaded employee {EmployeeId} via EmployeePlugin. CorrelationId={CorrelationId}",
            employeeId,
            context.CorrelationId);

        return StepResult.Success($"Loaded employee {employeeId}.", employee);
    }

    [GeneratedRegex(
        @"\bemployee\s+(?<id>\d+)\b|\bid\s*(?:#|=|:)?\s*(?<id>\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmployeeIdRegex();
}

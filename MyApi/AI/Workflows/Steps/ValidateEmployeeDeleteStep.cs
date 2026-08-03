using System.Text.Json;
using System.Text.RegularExpressions;
using MyApi.AI.Plugins;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;
using MyApi.DTO.Employee;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Validates that the employee targeted for deletion exists before requesting approval.
/// </summary>
public sealed partial class ValidateEmployeeDeleteStep : IWorkflowStep
{
    public const string EmployeeIdKey = "EmployeeId";
    public const string EmployeeJsonKey = "EmployeeJson";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly EmployeePlugin _employeePlugin;
    private readonly ILogger<ValidateEmployeeDeleteStep> _logger;

    public ValidateEmployeeDeleteStep(
        EmployeePlugin employeePlugin,
        ILogger<ValidateEmployeeDeleteStep> logger)
    {
        _employeePlugin = employeePlugin;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "ValidateEmployee";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var match = EmployeeIdRegex().Match(context.Harness.CurrentMessage);
        if (!match.Success || !int.TryParse(match.Groups["id"].Value, out var employeeId))
        {
            return StepResult.Failure("Could not determine which employee id to delete.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var payload = await _employeePlugin.GetEmployeeById(employeeId);
        if (payload.StartsWith("No employee", StringComparison.OrdinalIgnoreCase))
        {
            context.State.Set("FinalResponse", payload);
            return StepResult.Failure(payload);
        }

        var employee = JsonSerializer.Deserialize<Reademployeedto>(payload, JsonOptions);
        context.State.Set(EmployeeIdKey, employeeId);
        context.State.Set(EmployeeJsonKey, payload);
        if (employee is not null)
        {
            context.State.Set(EmployeeLookupStep.EmployeeVariableKey, employee);
        }

        _logger.LogInformation(
            "Validated employee {EmployeeId} for deletion. CorrelationId={CorrelationId}",
            employeeId,
            context.CorrelationId);

        return StepResult.Success($"Validated employee {employeeId}.", employee);
    }

    [GeneratedRegex(
        @"\bemployee\s+(?<id>\d+)\b|\bid\s*(?:#|=|:)?\s*(?<id>\d+)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex EmployeeIdRegex();
}

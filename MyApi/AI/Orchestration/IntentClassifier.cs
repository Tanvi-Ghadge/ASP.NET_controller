namespace MyApi.AI.Orchestration;

/// <summary>
/// Classifies a user message into a <see cref="UserIntent"/>.
/// Rule-based today; replaceable by an LLM planner without changing the orchestrator.
/// </summary>
public interface IIntentClassifier
{
    /// <summary>Classifies <paramref name="userMessage"/>.</summary>
    Task<UserIntent> ClassifyAsync(
        string userMessage,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight rule-based intent classifier for multi-agent routing.
/// </summary>
public sealed class IntentClassifier : IIntentClassifier
{
    private readonly ILogger<IntentClassifier> _logger;

    public IntentClassifier(ILogger<IntentClassifier> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<UserIntent> ClassifyAsync(
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = userMessage?.Trim() ?? string.Empty;
        var intent = ClassifyCore(text);

        _logger.LogInformation(
            "Intent classified as {Intent} for message length {Length}.",
            intent,
            text.Length);

        return Task.FromResult(intent);
    }

    private static UserIntent ClassifyCore(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return UserIntent.GeneralConversation;
        }

        if (ContainsAny(text, "delete", "remove") &&
            text.Contains("employee", StringComparison.OrdinalIgnoreCase))
        {
            return UserIntent.DeleteEmployee;
        }

        var payrollish = ContainsAny(text, "payroll", "salary", "payslip", "compensation", "bonus");
        var reportish = ContainsAny(text, "report", "summary", "analytics", "statistics", "stats");
        var hasEmployee = text.Contains("employee", StringComparison.OrdinalIgnoreCase);

        // "Generate employee payroll report" / "employee payroll report"
        if (payrollish && hasEmployee && (reportish || ContainsAny(text, "generate", "create")))
        {
            return UserIntent.EmployeePayrollReport;
        }

        // "Generate payroll report" / "payroll report and email HR"
        if (payrollish && (reportish || ContainsAny(text, "generate", "create", "email", "notify")))
        {
            return UserIntent.PayrollReport;
        }

        if (payrollish)
        {
            return UserIntent.PayrollReport;
        }

        if (ContainsAny(text, "report", "statistics", "stats") ||
            (ContainsAny(text, "generate", "create") &&
             hasEmployee &&
             ContainsAny(text, "report", "summary", "overview")))
        {
            return UserIntent.EmployeeReport;
        }

        if (text.Contains("generate", StringComparison.OrdinalIgnoreCase) &&
            text.Contains("report", StringComparison.OrdinalIgnoreCase))
        {
            return UserIntent.EmployeeReport;
        }

        if (ContainsAny(text, "show", "get", "find", "fetch", "lookup", "who is") &&
            hasEmployee)
        {
            return UserIntent.EmployeeLookup;
        }

        if (hasEmployee &&
            System.Text.RegularExpressions.Regex.IsMatch(text, @"\d+"))
        {
            return UserIntent.EmployeeLookup;
        }

        return UserIntent.GeneralConversation;
    }

    private static bool ContainsAny(string text, params string[] tokens) =>
        tokens.Any(t => text.Contains(t, StringComparison.OrdinalIgnoreCase));
}

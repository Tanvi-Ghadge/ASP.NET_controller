namespace MyApi.AI.Orchestration;

/// <summary>
/// Coarse user intents understood by the classifier.
/// Extensible for future multi-agent planner taxonomies.
/// </summary>
public enum UserIntent
{
    /// <summary>Show/find a specific employee.</summary>
    EmployeeLookup = 0,

    /// <summary>Generate an employee report / statistics.</summary>
    EmployeeReport = 1,

    /// <summary>Delete/remove an employee.</summary>
    DeleteEmployee = 2,

    /// <summary>Open-ended conversation / preferences / unclear intent.</summary>
    GeneralConversation = 3,

    /// <summary>Generate payroll/compensation report (PayrollAgent path).</summary>
    PayrollReport = 4,

    /// <summary>Multi-agent employee payroll report (Employee → Payroll → Reporting).</summary>
    EmployeePayrollReport = 5
}

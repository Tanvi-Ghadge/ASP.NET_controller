using MyApi.AI.Harness;

namespace MyApi.AI.Agents.Employee;

/// <summary>
/// Backward-compatible marker for the employee domain agent.
/// Prefer <see cref="IDomainAgent"/> for new multi-agent code.
/// </summary>
public interface IEmployeeAgent : IDomainAgent
{
}

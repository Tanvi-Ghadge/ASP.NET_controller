namespace MyApi.AI.Models;

/// <summary>
/// Configuration for Microsoft Agent Framework (OpenAI provider).
/// Bound from the "AgentFramework" section in appsettings.
/// </summary>
public sealed class AgentFrameworkOptions
{
    public const string SectionName = "AgentFramework";

    /// <summary>OpenAI API key. Prefer user-secrets or environment variable OPENAI_API_KEY in production.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chat model / deployment name.</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>System instructions that define EmployeeAgent behavior and tool usage.</summary>
    public string Instructions { get; set; } =
        "You are a helpful assistant for an Employee Management system. " +
        "When users ask about employees, use the available tools to read or change live data. " +
        "Never invent employee records. Prefer tools over guessing. " +
        "A separate system message may include long-term user memories (preferences/profile). " +
        "Honor those memories across conversations — for example, if PreferredReportFormat is PDF, " +
        "generate or discuss reports as PDF and say so briefly. " +
        "After a tool returns data, respond in clear natural language summarizing the result.";

    /// <summary>Display name registered with the Agent Framework.</summary>
    public string AgentName { get; set; } = "EmployeeAgent";

    /// <summary>Short description of the agent's purpose.</summary>
    public string AgentDescription { get; set; } =
        "Conversational assistant that uses EmployeePlugin tools backed by EmployeeService.";
}

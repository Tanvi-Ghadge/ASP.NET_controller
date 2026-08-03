using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace MyApi.AI.Plugins;

/// <summary>
/// Reporting-domain tools. Formats analytics/summaries from provided data — does not access payroll repositories.
/// </summary>
public sealed class ReportingPlugin
{
    private readonly ILogger<ReportingPlugin> _logger;

    public ReportingPlugin(ILogger<ReportingPlugin> logger)
    {
        _logger = logger;
    }

    public IList<AITool> GetAITools() =>
    [
        AIFunctionFactory.Create(GenerateTextReport)
    ];

    [Description("Generates a text report from a title and body.")]
    public string GenerateTextReport(
        [Description("Report title.")] string title,
        [Description("Report body.")] string body)
    {
        _logger.LogInformation("ReportingPlugin.GenerateTextReport(title={Title})", title);
        return BuildReport(title, body);
    }

    public string BuildPayrollReport(string employeeJson, string payrollJson, string? preferredFormat = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Employee Payroll Report");
        if (!string.IsNullOrWhiteSpace(preferredFormat))
        {
            sb.AppendLine($"Preferred format: {preferredFormat}");
        }

        sb.AppendLine(new string('-', 48));
        sb.AppendLine("Employee:");
        sb.AppendLine(Pretty(employeeJson));
        sb.AppendLine();
        sb.AppendLine("Payroll:");
        sb.AppendLine(Pretty(payrollJson));
        return sb.ToString().TrimEnd();
    }

    public string BuildCompensationReport(string compensationJson, string? preferredFormat = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Compensation / Payroll Report");
        if (!string.IsNullOrWhiteSpace(preferredFormat))
        {
            sb.AppendLine($"Preferred format: {preferredFormat}");
        }

        sb.AppendLine(new string('-', 48));
        sb.AppendLine(Pretty(compensationJson));
        return sb.ToString().TrimEnd();
    }

    private static string BuildReport(string title, string body) =>
        $"{title}{Environment.NewLine}{new string('-', Math.Min(48, title.Length))}{Environment.NewLine}{body}";

    private static string Pretty(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}

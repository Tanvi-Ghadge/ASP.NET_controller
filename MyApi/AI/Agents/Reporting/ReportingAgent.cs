using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using MyApi.AI.A2A;
using MyApi.AI.Agents.Payroll;
using MyApi.AI.Harness;
using MyApi.AI.Models;
using MyApi.AI.Plugins;
using MyApi.AI.Streaming;
using OpenAI;
using OpenAI.Chat;
using AgentResponse = MyApi.AI.A2A.AgentResponse;

namespace MyApi.AI.Agents.Reporting;

/// <summary>
/// Reporting-domain agent: statistics, summaries, and report generation.
/// Obtains payroll/employee data only through <see cref="IAgentCommunication"/> — never via foreign services.
/// </summary>
public sealed class ReportingAgent : IDomainAgent
{
    public const string Key = "reporting-agent";

    private readonly AIAgent _agent;
    private readonly ReportingPlugin _plugin;
    private readonly IAgentCommunication _a2a;
    private readonly IStreamingService _streaming;
    private readonly ILogger<ReportingAgent> _logger;

    public ReportingAgent(
        OpenAIClient openAiClient,
        IOptions<AgentFrameworkOptions> options,
        ReportingPlugin plugin,
        IAgentCommunication a2a,
        IStreamingService streaming,
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        ILogger<ReportingAgent> logger)
    {
        _plugin = plugin;
        _a2a = a2a;
        _streaming = streaming;
        _logger = logger;

        var opts = options.Value;
        _agent = openAiClient
            .GetChatClient(opts.Model)
            .AsAIAgent(
                instructions:
                    "You are the ReportingAgent. Produce summaries, statistics, and text reports. " +
                    "Do not invent payroll figures; ask collaborating agents via structured workflows when data is missing.",
                name: "ReportingAgent",
                description: "Reporting domain agent",
                tools: plugin.GetAITools(),
                loggerFactory: loggerFactory,
                services: services);

        _logger.LogInformation("ReportingAgent initialized with {ToolCount} tools.", plugin.GetAITools().Count);
    }

    public string AgentKey => Key;
    public string AgentName => "ReportingAgent";
    public string Domain => "Reporting";

    /// <inheritdoc />
    public Task<string> RunAsync(HarnessContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        EnsurePrepared(context);
        return AgentStreamingRunner.RunWithStreamingAsync(
            _agent,
            context,
            _streaming,
            AgentKey,
            AgentName,
            _logger,
            context.CancellationToken);
    }

    /// <inheritdoc />
    public async Task<AgentResponse> HandleAsync(AgentRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            switch (request.Operation.ToLowerInvariant())
            {
                case "generatepayrollreport":
                {
                    var employeeJson = ExtractString(request.RequestData, "employeeJson", "employee");
                    var payrollJson = ExtractString(request.RequestData, "payrollJson", "payroll");
                    var preferredFormat = ExtractString(request.RequestData, "preferredFormat", "format");

                    if (string.IsNullOrWhiteSpace(payrollJson))
                    {
                        var employeeId = ExtractInt(request.RequestData, "employeeId", "id");
                        if (employeeId is null)
                        {
                            return AgentResponse.Failure(
                                AgentName,
                                "payrollJson or employeeId is required to generate a payroll report.",
                                sw.Elapsed);
                        }

                        _logger.LogInformation(
                            "Agent Delegation. From={From}, To={To}, Operation=GetPayroll, CorrelationId={CorrelationId}",
                            AgentName,
                            PayrollAgent.Key,
                            request.CorrelationId);

                        var payrollResponse = await _a2a.SendAsync(
                            new AgentRequest
                            {
                                AgentName = PayrollAgent.Key,
                                Operation = "GetPayroll",
                                CorrelationId = request.CorrelationId,
                                SessionId = request.SessionId,
                                MemoryContext = request.MemoryContext,
                                RequestData = new Dictionary<string, object?> { ["employeeId"] = employeeId.Value },
                                ExecutionMetadata = request.ExecutionMetadata
                            },
                            cancellationToken);

                        if (!payrollResponse.Succeeded)
                        {
                            return AgentResponse.Failure(
                                AgentName,
                                payrollResponse.Errors.FirstOrDefault() ?? "PayrollAgent failed.",
                                sw.Elapsed);
                        }

                        payrollJson = payrollResponse.TextOutput ?? JsonSerializer.Serialize(payrollResponse.Payload);
                    }

                    employeeJson ??= "{}";
                    var report = _plugin.BuildPayrollReport(employeeJson, payrollJson!, preferredFormat);
                    sw.Stop();
                    return AgentResponse.Success(AgentName, report, report, sw.Elapsed);
                }
                case "generatecompensationreport":
                {
                    var compensationJson = ExtractString(request.RequestData, "compensationJson", "summary");
                    var preferredFormat = ExtractString(request.RequestData, "preferredFormat", "format");

                    if (string.IsNullOrWhiteSpace(compensationJson))
                    {
                        _logger.LogInformation(
                            "Agent Delegation. From={From}, To={To}, Operation=GetCompensationSummary, CorrelationId={CorrelationId}",
                            AgentName,
                            PayrollAgent.Key,
                            request.CorrelationId);

                        var payrollResponse = await _a2a.SendAsync(
                            new AgentRequest
                            {
                                AgentName = PayrollAgent.Key,
                                Operation = "GetCompensationSummary",
                                CorrelationId = request.CorrelationId,
                                SessionId = request.SessionId,
                                MemoryContext = request.MemoryContext,
                                ExecutionMetadata = request.ExecutionMetadata
                            },
                            cancellationToken);

                        if (!payrollResponse.Succeeded)
                        {
                            return AgentResponse.Failure(
                                AgentName,
                                payrollResponse.Errors.FirstOrDefault() ?? "PayrollAgent failed.",
                                sw.Elapsed);
                        }

                        compensationJson = payrollResponse.TextOutput ?? JsonSerializer.Serialize(payrollResponse.Payload);
                    }

                    var report = _plugin.BuildCompensationReport(compensationJson!, preferredFormat);
                    sw.Stop();
                    return AgentResponse.Success(AgentName, report, report, sw.Elapsed);
                }
                case "generatetextreport":
                {
                    var title = ExtractString(request.RequestData, "title") ?? "Report";
                    var body = ExtractString(request.RequestData, "body") ?? string.Empty;
                    var report = _plugin.GenerateTextReport(title, body);
                    sw.Stop();
                    return AgentResponse.Success(AgentName, report, report, sw.Elapsed);
                }
                default:
                    return AgentResponse.Failure(
                        AgentName,
                        $"Unsupported operation '{request.Operation}' for ReportingAgent.",
                        sw.Elapsed);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            return AgentResponse.Failure(AgentName, ex.Message, sw.Elapsed);
        }
    }

    private static void EnsurePrepared(HarnessContext context)
    {
        if (context.PreparedMessages is null || context.PreparedMessages.Count == 0)
        {
            throw new InvalidOperationException("HarnessContext.PreparedMessages is empty.");
        }
    }

    private static string? ExtractString(object? data, params string[] keys)
    {
        if (data is null)
        {
            return null;
        }

        if (data is string s)
        {
            return s;
        }

        if (data is JsonElement el)
        {
            foreach (var key in keys)
            {
                if (el.TryGetProperty(key, out var prop))
                {
                    return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.GetRawText();
                }
            }
        }

        if (data is IDictionary<string, object?> dict)
        {
            foreach (var key in keys)
            {
                if (dict.TryGetValue(key, out var value) && value is not null)
                {
                    return value as string ?? JsonSerializer.Serialize(value);
                }
            }
        }

        return null;
    }

    private static int? ExtractInt(object? data, params string[] keys)
    {
        if (data is null)
        {
            return null;
        }

        if (data is int i)
        {
            return i;
        }

        if (data is JsonElement el)
        {
            foreach (var key in keys)
            {
                if (el.TryGetProperty(key, out var prop) && prop.TryGetInt32(out var value))
                {
                    return value;
                }
            }
        }

        if (data is IDictionary<string, object?> dict)
        {
            foreach (var key in keys)
            {
                if (dict.TryGetValue(key, out var value) && value is not null &&
                    int.TryParse(value.ToString(), out var parsed))
                {
                    return parsed;
                }
            }
        }

        return null;
    }
}

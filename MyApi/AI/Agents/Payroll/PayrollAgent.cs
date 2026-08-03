using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using MyApi.AI.A2A;
using MyApi.AI.Harness;
using MyApi.AI.Models;
using MyApi.AI.Plugins;
using MyApi.AI.Streaming;
using OpenAI;
using OpenAI.Chat;
using AgentResponse = MyApi.AI.A2A.AgentResponse;

namespace MyApi.AI.Agents.Payroll;

/// <summary>
/// Payroll-domain agent: salary, compensation, payslip-style data only.
/// Does not own employee CRUD or reporting — collaborate via A2A when needed.
/// </summary>
public sealed class PayrollAgent : IDomainAgent
{
    public const string Key = "payroll-agent";

    private readonly AIAgent _agent;
    private readonly PayrollPlugin _plugin;
    private readonly IStreamingService _streaming;
    private readonly ILogger<PayrollAgent> _logger;

    public PayrollAgent(
        OpenAIClient openAiClient,
        IOptions<AgentFrameworkOptions> options,
        PayrollPlugin plugin,
        IStreamingService streaming,
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        ILogger<PayrollAgent> logger)
    {
        _plugin = plugin;
        _streaming = streaming;
        _logger = logger;

        var opts = options.Value;
        _agent = openAiClient
            .GetChatClient(opts.Model)
            .AsAIAgent(
                instructions:
                    "You are the PayrollAgent. Answer only salary, payroll, compensation, bonus, and payslip questions. " +
                    "Use payroll tools. Do not perform employee CRUD or send emails.",
                name: "PayrollAgent",
                description: "Payroll domain agent",
                tools: plugin.GetAITools(),
                loggerFactory: loggerFactory,
                services: services);

        _logger.LogInformation("PayrollAgent initialized with {ToolCount} tools.", plugin.GetAITools().Count);
    }

    public string AgentKey => Key;
    public string AgentName => "PayrollAgent";
    public string Domain => "Payroll";

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
                case "getpayroll":
                case "loadpayroll":
                {
                    var id = ExtractInt(request.RequestData, "employeeId", "id");
                    if (id is null)
                    {
                        return AgentResponse.Failure(AgentName, "employeeId is required.", sw.Elapsed);
                    }

                    var record = await _plugin.GetPayrollRecordAsync(id.Value);
                    if (record is null)
                    {
                        return AgentResponse.Failure(AgentName, $"No payroll found for employee {id}.", sw.Elapsed);
                    }

                    var json = JsonSerializer.Serialize(record);
                    sw.Stop();
                    return AgentResponse.Success(AgentName, record, json, sw.Elapsed);
                }
                case "getcompensationsummary":
                case "compensationsummary":
                {
                    var summary = await _plugin.GetCompensationSummaryAsync();
                    var json = JsonSerializer.Serialize(summary);
                    sw.Stop();
                    return AgentResponse.Success(AgentName, summary, json, sw.Elapsed);
                }
                default:
                    return AgentResponse.Failure(
                        AgentName,
                        $"Unsupported operation '{request.Operation}' for PayrollAgent.",
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

        if (int.TryParse(data.ToString(), out var direct))
        {
            return direct;
        }

        return null;
    }
}

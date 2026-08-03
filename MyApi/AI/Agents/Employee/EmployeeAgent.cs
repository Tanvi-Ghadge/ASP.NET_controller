using System.Diagnostics;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Options;
using MyApi.AI.A2A;
using MyApi.AI.Harness;
using MyApi.AI.Models;
using MyApi.AI.Plugins;
using MyApi.AI.Streaming;
using MyApi.DTO.Employee;
using OpenAI;
using OpenAI.Chat;
using AgentResponse = MyApi.AI.A2A.AgentResponse;

namespace MyApi.AI.Agents.Employee;

/// <summary>
/// Employee-domain agent: lookup/CRUD/search only. Owns employee capabilities via <see cref="EmployeePlugin"/>.
/// </summary>
public sealed class EmployeeAgent : IDomainAgent, IEmployeeAgent
{
    public const string Key = "employee-agent";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AIAgent _agent;
    private readonly EmployeePlugin _plugin;
    private readonly IStreamingService _streaming;
    private readonly ILogger<EmployeeAgent> _logger;

    public EmployeeAgent(
        OpenAIClient openAiClient,
        IOptions<AgentFrameworkOptions> options,
        EmployeePlugin plugin,
        IStreamingService streaming,
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        ILogger<EmployeeAgent> logger)
    {
        _plugin = plugin;
        _streaming = streaming;
        _logger = logger;

        var opts = options.Value;
        _agent = openAiClient
            .GetChatClient(opts.Model)
            .AsAIAgent(
                instructions:
                    "You are the EmployeeAgent. You only answer employee lookup, CRUD, and search questions. " +
                    "Use employee tools. Do not invent payroll or notification behavior.",
                name: "EmployeeAgent",
                description: "Employee domain agent",
                tools: plugin.GetAITools(),
                loggerFactory: loggerFactory,
                services: services);

        _logger.LogInformation("EmployeeAgent initialized with {ToolCount} tools.", plugin.GetAITools().Count);
    }

    public string AgentKey => Key;
    public string AgentName => "EmployeeAgent";
    public string Domain => "Employee";

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
                case "getemployee":
                case "loademployee":
                {
                    var id = ExtractInt(request.RequestData, "employeeId", "id");
                    if (id is null)
                    {
                        return AgentResponse.Failure(AgentName, "employeeId is required.", sw.Elapsed);
                    }

                    var json = await _plugin.GetEmployeeById(id.Value);
                    if (json.StartsWith("No employee", StringComparison.OrdinalIgnoreCase))
                    {
                        return AgentResponse.Failure(AgentName, json, sw.Elapsed);
                    }

                    var dto = JsonSerializer.Deserialize<Reademployeedto>(json, JsonOptions);
                    sw.Stop();
                    return AgentResponse.Success(AgentName, dto, json, sw.Elapsed);
                }
                case "listemployees":
                {
                    var json = await _plugin.GetAllEmployees();
                    sw.Stop();
                    return AgentResponse.Success(AgentName, json, json, sw.Elapsed);
                }
                default:
                    return AgentResponse.Failure(
                        AgentName,
                        $"Unsupported operation '{request.Operation}' for EmployeeAgent.",
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

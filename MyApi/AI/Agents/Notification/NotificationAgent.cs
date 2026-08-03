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

namespace MyApi.AI.Agents.Notification;

/// <summary>
/// Notification-domain agent: email / SMS / notifications only.
/// </summary>
public sealed class NotificationAgent : IDomainAgent
{
    public const string Key = "notification-agent";

    private readonly AIAgent _agent;
    private readonly NotificationPlugin _plugin;
    private readonly IStreamingService _streaming;
    private readonly ILogger<NotificationAgent> _logger;

    public NotificationAgent(
        OpenAIClient openAiClient,
        IOptions<AgentFrameworkOptions> options,
        NotificationPlugin plugin,
        IStreamingService streaming,
        ILoggerFactory loggerFactory,
        IServiceProvider services,
        ILogger<NotificationAgent> logger)
    {
        _plugin = plugin;
        _streaming = streaming;
        _logger = logger;

        var opts = options.Value;
        _agent = openAiClient
            .GetChatClient(opts.Model)
            .AsAIAgent(
                instructions:
                    "You are the NotificationAgent. Send emails and notifications only. " +
                    "Do not look up employees or calculate payroll.",
                name: "NotificationAgent",
                description: "Notification domain agent",
                tools: plugin.GetAITools(),
                loggerFactory: loggerFactory,
                services: services);

        _logger.LogInformation("NotificationAgent initialized with {ToolCount} tools.", plugin.GetAITools().Count);
    }

    public string AgentKey => Key;
    public string AgentName => "NotificationAgent";
    public string Domain => "Notification";

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
                case "sendemail":
                case "notify":
                case "emailreport":
                {
                    var email = ExtractString(request.RequestData, "email", "to", "recipient") ?? "hr@company.com";
                    var subject = ExtractString(request.RequestData, "subject") ?? "Notification";
                    var body = ExtractString(request.RequestData, "body", "report", "message") ?? string.Empty;

                    var result = await _plugin.SendEmailNotification(email, subject, body);
                    sw.Stop();
                    return AgentResponse.Success(
                        AgentName,
                        new { email, subject, result },
                        result,
                        sw.Elapsed);
                }
                default:
                    return AgentResponse.Failure(
                        AgentName,
                        $"Unsupported operation '{request.Operation}' for NotificationAgent.",
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
                if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
                {
                    return prop.GetString();
                }
            }
        }

        if (data is IDictionary<string, object?> dict)
        {
            foreach (var key in keys)
            {
                if (dict.TryGetValue(key, out var value) && value is not null)
                {
                    return value.ToString();
                }
            }
        }

        return null;
    }
}

using Microsoft.AspNetCore.SignalR;

namespace MyApi.AI.Streaming;

/// <summary>
/// SignalR hub for AG-UI clients (React / Blazor / Angular / Teams / Copilot).
/// Clients join a session group to receive only their execution events.
/// Mapped at <c>/hubs/agent</c>.
/// </summary>
public sealed class AgentHub : Hub
{
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(ILogger<AgentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>SignalR group name for a conversation session.</summary>
    public static string SessionGroup(string sessionId) => $"session:{sessionId}";

    /// <summary>SignalR group name for a single execution / correlation id.</summary>
    public static string ExecutionGroup(string correlationId) => $"execution:{correlationId}";

    /// <summary>Join the session fan-out group.</summary>
    public async Task JoinSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new HubException("sessionId is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
        _logger.LogInformation(
            "SignalR client joined session group. ConnectionId={ConnectionId}, SessionId={SessionId}",
            Context.ConnectionId,
            sessionId);
    }

    /// <summary>Leave the session fan-out group.</summary>
    public async Task LeaveSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SessionGroup(sessionId));
    }

    /// <summary>Subscribe to a single execution (useful before session id is known).</summary>
    public async Task JoinExecution(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            throw new HubException("correlationId is required.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ExecutionGroup(correlationId));
        _logger.LogInformation(
            "SignalR client joined execution group. ConnectionId={ConnectionId}, CorrelationId={CorrelationId}",
            Context.ConnectionId,
            correlationId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR connected. ConnectionId={ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "SignalR disconnected. ConnectionId={ConnectionId}, Error={Error}",
            Context.ConnectionId,
            exception?.Message);
        return base.OnDisconnectedAsync(exception);
    }
}

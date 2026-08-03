namespace MyApi.AI.Sessions;

/// <summary>
/// Application-owned conversation state for multi-turn chat.
/// This is distinct from <c>Microsoft.Agents.AI.AgentSession</c>:
/// the Microsoft Agent Framework agent remains stateless; all durable
/// (or in-memory) conversation memory lives here.
/// </summary>
public sealed class AgentSession
{
    /// <summary>Client-visible conversation identifier.</summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>When the session was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When the session last received activity (UTC).</summary>
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Ordered conversation history (user + assistant + optional system).</summary>
    public List<SessionMessage> Messages { get; set; } = [];

    /// <summary>Extensible metadata for future use.</summary>
    public SessionMetadata Metadata { get; set; } = new();

    /// <summary>User messages only.</summary>
    public IEnumerable<SessionMessage> UserMessages =>
        Messages.Where(m => m.Role == SessionMessageRole.User);

    /// <summary>Assistant messages only.</summary>
    public IEnumerable<SessionMessage> AssistantMessages =>
        Messages.Where(m => m.Role == SessionMessageRole.Assistant);

    /// <summary>Appends a user message and refreshes last activity.</summary>
    public void AddUserMessage(string content)
    {
        Messages.Add(SessionMessage.FromUser(content));
        Touch();
    }

    /// <summary>Appends an assistant message and refreshes last activity.</summary>
    public void AddAssistantMessage(string content)
    {
        Messages.Add(SessionMessage.FromAssistant(content));
        Touch();
    }

    /// <summary>Updates <see cref="LastActivityAt"/> to now (UTC).</summary>
    public void Touch() => LastActivityAt = DateTimeOffset.UtcNow;
}

namespace MyApi.AI.Sessions;

/// <summary>
/// A single turn message stored in an <see cref="AgentSession"/>.
/// </summary>
public sealed class SessionMessage
{
    /// <summary>Unique message identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Who produced the message.</summary>
    public SessionMessageRole Role { get; set; }

    /// <summary>Text content of the message.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>When the message was recorded (UTC).</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Creates a user message.</summary>
    public static SessionMessage FromUser(string content) => new()
    {
        Role = SessionMessageRole.User,
        Content = content,
        Timestamp = DateTimeOffset.UtcNow
    };

    /// <summary>Creates an assistant message.</summary>
    public static SessionMessage FromAssistant(string content) => new()
    {
        Role = SessionMessageRole.Assistant,
        Content = content,
        Timestamp = DateTimeOffset.UtcNow
    };
}

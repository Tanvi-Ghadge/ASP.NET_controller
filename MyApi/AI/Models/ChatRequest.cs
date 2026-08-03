using System.ComponentModel.DataAnnotations;

namespace MyApi.AI.Models;

/// <summary>
/// Incoming chat payload for POST /api/chat.
/// </summary>
public sealed class ChatRequest
{
    /// <summary>
    /// Existing conversation id. Empty or omitted starts a new session.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Stable user id for long-term memory across sessions.
    /// When omitted, <c>default-user</c> is used so Swagger demos still share memory.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>User utterance for this turn.</summary>
    [Required(ErrorMessage = "Message is required.")]
    [MinLength(1)]
    public string Message { get; set; } = string.Empty;
}

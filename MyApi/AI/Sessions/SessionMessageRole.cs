namespace MyApi.AI.Sessions;

/// <summary>
/// Role of a message within an application-owned conversation session.
/// </summary>
public enum SessionMessageRole
{
    User = 0,
    Assistant = 1,
    System = 2
}

namespace MyApi.AI.Memory;

/// <summary>
/// Categories of long-term memory. Extensible for future memory kinds.
/// </summary>
public enum MemoryType
{
    /// <summary>Durable user preference (e.g. PreferredReportFormat = PDF).</summary>
    UserPreference = 0,

    /// <summary>Stable profile attributes (e.g. department affiliation).</summary>
    UserProfile = 1,

    /// <summary>Facts learned about the user over time.</summary>
    LearnedFact = 2,

    /// <summary>Organization / domain facts relevant to the user.</summary>
    BusinessFact = 3,

    /// <summary>Short-lived preference that may expire or be overridden.</summary>
    TemporaryPreference = 4
}

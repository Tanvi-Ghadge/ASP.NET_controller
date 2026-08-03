namespace MyApi.AI.Sessions;

/// <summary>
/// Extensible bag of session-scoped metadata for future features
/// (tenant id, channel, feature flags, etc.).
/// </summary>
public sealed class SessionMetadata
{
    /// <summary>Arbitrary string key/value pairs associated with the session.</summary>
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets a metadata value if present.</summary>
    public bool TryGetValue(string key, out string? value) => Values.TryGetValue(key, out value);

    /// <summary>Sets or overwrites a metadata value.</summary>
    public void Set(string key, string value) => Values[key] = value;
}

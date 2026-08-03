namespace MyApi.models.entities;

/// <summary>
/// EF Core entity for long-term AI memories persisted in SQL Server.
/// </summary>
public class AiMemory
{
    public Guid MemoryId { get; set; }

    public required string UserId { get; set; }

    public required string Key { get; set; }

    public required string Value { get; set; }

    /// <summary>Stored as int matching <c>MyApi.AI.Memory.MemoryType</c>.</summary>
    public int Type { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

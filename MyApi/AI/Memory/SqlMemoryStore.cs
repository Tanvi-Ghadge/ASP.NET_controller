using Microsoft.EntityFrameworkCore;
using MyApi.data;
using MyApi.models.entities;

namespace MyApi.AI.Memory;

/// <summary>
/// SQL Server-backed <see cref="IMemoryStore"/> using EF Core.
/// Swap this implementation for Redis/Cosmos/vector stores without changing the agent.
/// </summary>
public sealed class SqlMemoryStore : IMemoryStore
{
    private readonly Dbcontext _db;
    private readonly ILogger<SqlMemoryStore> _logger;

    public SqlMemoryStore(Dbcontext db, ILogger<SqlMemoryStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MemoryRecord> SaveMemoryAsync(
        MemoryRecord memory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(memory);

        if (string.IsNullOrWhiteSpace(memory.UserId))
        {
            throw new ArgumentException("UserId is required.", nameof(memory));
        }

        if (string.IsNullOrWhiteSpace(memory.Key))
        {
            throw new ArgumentException("Key is required.", nameof(memory));
        }

        var existing = await _db.AiMemories
            .FirstOrDefaultAsync(
                m => m.UserId == memory.UserId && m.Key == memory.Key,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var entity = new AiMemory
            {
                MemoryId = memory.MemoryId == Guid.Empty ? Guid.NewGuid() : memory.MemoryId,
                UserId = memory.UserId.Trim(),
                Key = memory.Key.Trim(),
                Value = memory.Value?.Trim() ?? string.Empty,
                Type = (int)memory.Type,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.AiMemories.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Saved new memory {Key} for user {UserId} ({MemoryId}).",
                entity.Key,
                entity.UserId,
                entity.MemoryId);

            return ToRecord(entity);
        }

        existing.Value = memory.Value?.Trim() ?? string.Empty;
        existing.Type = (int)memory.Type;
        existing.UpdatedAt = now;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated memory {Key} for user {UserId} ({MemoryId}).",
            existing.Key,
            existing.UserId,
            existing.MemoryId);

        return ToRecord(existing);
    }

    /// <inheritdoc />
    public async Task<MemoryRecord?> UpdateMemoryAsync(
        MemoryRecord memory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(memory);

        var entity = await _db.AiMemories
            .FirstOrDefaultAsync(m => m.MemoryId == memory.MemoryId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Key = memory.Key.Trim();
        entity.Value = memory.Value?.Trim() ?? string.Empty;
        entity.Type = (int)memory.Type;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteMemoryAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.AiMemories
            .FirstOrDefaultAsync(m => m.MemoryId == memoryId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _db.AiMemories.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    public async Task<MemoryRecord?> GetMemoryAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.AiMemories
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MemoryId == memoryId, cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<MemoryRecord?> GetMemoryByKeyAsync(
        string userId,
        string key,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.AiMemories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.UserId == userId && m.Key == key,
                cancellationToken);

        return entity is null ? null : ToRecord(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MemoryRecord>> SearchMemoryAsync(
        string userId,
        string? query = null,
        MemoryType? type = null,
        CancellationToken cancellationToken = default)
    {
        var q = _db.AiMemories.AsNoTracking().Where(m => m.UserId == userId);

        if (type.HasValue)
        {
            var typeValue = (int)type.Value;
            q = q.Where(m => m.Type == typeValue);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(m =>
                m.Key.ToLower().Contains(term) ||
                m.Value.ToLower().Contains(term));
        }

        var entities = await q
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MemoryRecord>> GetUserMemoriesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var entities = await _db.AiMemories
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToRecord).ToList();
    }

    private static MemoryRecord ToRecord(AiMemory entity) => new()
    {
        MemoryId = entity.MemoryId,
        UserId = entity.UserId,
        Key = entity.Key,
        Value = entity.Value,
        Type = (MemoryType)entity.Type,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}

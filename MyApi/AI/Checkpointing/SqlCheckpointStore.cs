using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyApi.data;
using MyApi.models.entities;

namespace MyApi.AI.Checkpointing;

/// <summary>
/// SQL Server checkpoint store via EF Core. Never keeps paused workflows only in memory.
/// </summary>
public sealed class SqlCheckpointStore : ICheckpointStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly Dbcontext _db;
    private readonly ILogger<SqlCheckpointStore> _logger;

    public SqlCheckpointStore(Dbcontext db, ILogger<SqlCheckpointStore> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        var existing = await _db.WorkflowCheckpoints
            .FirstOrDefaultAsync(c => c.WorkflowInstanceId == checkpoint.WorkflowInstanceId, cancellationToken);

        if (existing is null)
        {
            _db.WorkflowCheckpoints.Add(ToEntity(checkpoint));
            _logger.LogInformation(
                "Checkpoint Created. CheckpointId={CheckpointId}, WorkflowInstanceId={WorkflowInstanceId}, ResumeStepIndex={ResumeStepIndex}, Status={Status}",
                checkpoint.CheckpointId,
                checkpoint.WorkflowInstanceId,
                checkpoint.ResumeStepIndex,
                checkpoint.Status);
        }
        else
        {
            existing.ResumeStepIndex = checkpoint.ResumeStepIndex;
            existing.CompletedStepsJson = JsonSerializer.Serialize(checkpoint.CompletedSteps, JsonOptions);
            existing.VariablesJson = JsonSerializer.Serialize(checkpoint.Variables, JsonOptions);
            existing.ExecutionMetadataJson = JsonSerializer.Serialize(checkpoint.ExecutionMetadata, JsonOptions);
            existing.MemoryJson = checkpoint.MemoryJson;
            existing.Status = checkpoint.Status;
            existing.ApprovalRequestId = checkpoint.ApprovalRequestId;
            existing.SelectedAgentKey = checkpoint.SelectedAgentKey;
            existing.CurrentMessage = checkpoint.CurrentMessage;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation(
                "Checkpoint Updated. CheckpointId={CheckpointId}, WorkflowInstanceId={WorkflowInstanceId}, Status={Status}",
                existing.CheckpointId,
                existing.WorkflowInstanceId,
                existing.Status);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowCheckpoint?> GetByInstanceIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.WorkflowCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.WorkflowInstanceId == workflowInstanceId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        _logger.LogInformation(
            "Checkpoint Loaded. CheckpointId={CheckpointId}, WorkflowInstanceId={WorkflowInstanceId}, ResumeStepIndex={ResumeStepIndex}",
            entity.CheckpointId,
            entity.WorkflowInstanceId,
            entity.ResumeStepIndex);

        return ToModel(entity);
    }

    /// <inheritdoc />
    public async Task<WorkflowCheckpoint?> GetByCheckpointIdAsync(
        Guid checkpointId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.WorkflowCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CheckpointId == checkpointId, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(
        Guid workflowInstanceId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.WorkflowCheckpoints
            .FirstOrDefaultAsync(c => c.WorkflowInstanceId == workflowInstanceId, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static WorkflowCheckpointEntity ToEntity(WorkflowCheckpoint checkpoint) =>
        new()
        {
            CheckpointId = checkpoint.CheckpointId == Guid.Empty ? Guid.NewGuid() : checkpoint.CheckpointId,
            WorkflowInstanceId = checkpoint.WorkflowInstanceId,
            WorkflowDefinitionId = checkpoint.WorkflowDefinitionId,
            WorkflowName = checkpoint.WorkflowName,
            ResumeStepIndex = checkpoint.ResumeStepIndex,
            CompletedStepsJson = JsonSerializer.Serialize(checkpoint.CompletedSteps, JsonOptions),
            VariablesJson = JsonSerializer.Serialize(checkpoint.Variables, JsonOptions),
            SessionId = checkpoint.SessionId,
            UserId = checkpoint.UserId,
            CorrelationId = checkpoint.CorrelationId,
            CurrentMessage = checkpoint.CurrentMessage,
            SelectedAgentKey = checkpoint.SelectedAgentKey,
            MemoryJson = checkpoint.MemoryJson,
            ExecutionMetadataJson = JsonSerializer.Serialize(checkpoint.ExecutionMetadata, JsonOptions),
            Status = checkpoint.Status,
            ApprovalRequestId = checkpoint.ApprovalRequestId,
            CreatedAt = checkpoint.CreatedAt,
            UpdatedAt = checkpoint.UpdatedAt
        };

    private static WorkflowCheckpoint ToModel(WorkflowCheckpointEntity entity) =>
        new()
        {
            CheckpointId = entity.CheckpointId,
            WorkflowInstanceId = entity.WorkflowInstanceId,
            WorkflowDefinitionId = entity.WorkflowDefinitionId,
            WorkflowName = entity.WorkflowName,
            ResumeStepIndex = entity.ResumeStepIndex,
            CompletedSteps = DeserializeList(entity.CompletedStepsJson),
            Variables = DeserializeDict(entity.VariablesJson),
            SessionId = entity.SessionId,
            UserId = entity.UserId,
            CorrelationId = entity.CorrelationId,
            CurrentMessage = entity.CurrentMessage,
            SelectedAgentKey = entity.SelectedAgentKey,
            MemoryJson = entity.MemoryJson,
            ExecutionMetadata = DeserializeDict(entity.ExecutionMetadataJson),
            Status = entity.Status,
            ApprovalRequestId = entity.ApprovalRequestId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };

    private static IReadOnlyList<string> DeserializeList(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
    }

    private static Dictionary<string, object?> DeserializeDict(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions)
                  ?? new Dictionary<string, JsonElement>();

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, element) in raw)
        {
            result[key] = JsonElementToObject(element);
        }

        return result;
    }

    private static object? JsonElementToObject(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var i) => i,
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number when element.TryGetDecimal(out var d) => d,
            JsonValueKind.Number => element.GetDouble(),
            _ => element.GetRawText()
        };
}

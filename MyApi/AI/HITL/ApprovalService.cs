using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyApi.data;
using MyApi.models.entities;

namespace MyApi.AI.HITL;

/// <summary>
/// SQL-backed approval service with audit history. Independent of checkpoint / engine internals.
/// </summary>
public sealed class ApprovalService : IApprovalService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dbcontext _db;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(Dbcontext db, ILogger<ApprovalService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApprovalResponse> RequestAsync(
        ApprovalRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = new ApprovalRequestEntity
        {
            ApprovalRequestId = Guid.NewGuid(),
            WorkflowInstanceId = request.WorkflowInstanceId,
            WorkflowDefinitionId = request.WorkflowDefinitionId,
            CorrelationId = request.CorrelationId,
            SessionId = request.SessionId,
            UserId = request.UserId,
            Title = request.Title,
            Description = request.Description,
            Status = (int)ApprovalStatus.Pending,
            CheckpointId = request.CheckpointId,
            PayloadJson = JsonSerializer.Serialize(request.Payload, JsonOptions),
            ApprovalGroup = request.ApprovalGroup,
            RequiredApprovals = request.RequiredApprovals,
            RequestedAt = DateTimeOffset.UtcNow,
            ExpiresAt = request.ExpiresAt
        };

        entity.History.Add(new ApprovalHistoryEntity
        {
            HistoryId = Guid.NewGuid(),
            ApprovalRequestId = entity.ApprovalRequestId,
            Action = "Requested",
            Actor = request.UserId,
            Notes = request.Description,
            Timestamp = DateTimeOffset.UtcNow
        });

        _db.ApprovalRequests.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Approval Requested. ApprovalRequestId={ApprovalRequestId}, WorkflowInstanceId={WorkflowInstanceId}, Title={Title}",
            entity.ApprovalRequestId,
            entity.WorkflowInstanceId,
            entity.Title);

        return ToResponse(entity);
    }

    /// <inheritdoc />
    public Task<ApprovalResponse> ApproveAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest decision,
        CancellationToken cancellationToken = default) =>
        DecideAsync(workflowInstanceId, ApprovalDecision.Approve, decision, cancellationToken);

    /// <inheritdoc />
    public Task<ApprovalResponse> RejectAsync(
        Guid workflowInstanceId,
        ApprovalDecisionRequest decision,
        CancellationToken cancellationToken = default) =>
        DecideAsync(workflowInstanceId, ApprovalDecision.Reject, decision, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApprovalResponse>> GetPendingAsync(
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ApprovalRequests
            .AsNoTracking()
            .Where(a => a.Status == (int)ApprovalStatus.Pending);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(a => a.UserId == userId);
        }

        var list = await query
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(cancellationToken);

        return list.Select(ToResponse).ToList();
    }

    /// <inheritdoc />
    public async Task<ApprovalResponse?> GetByWorkflowInstanceAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.ApprovalRequests
            .AsNoTracking()
            .Where(a => a.WorkflowInstanceId == workflowInstanceId)
            .OrderByDescending(a => a.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToResponse(entity);
    }

    private async Task<ApprovalResponse> DecideAsync(
        Guid workflowInstanceId,
        ApprovalDecision decision,
        ApprovalDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await _db.ApprovalRequests
            .Include(a => a.History)
            .Where(a => a.WorkflowInstanceId == workflowInstanceId && a.Status == (int)ApprovalStatus.Pending)
            .OrderByDescending(a => a.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException(
                $"No pending approval found for workflow instance '{workflowInstanceId}'.");
        }

        var actor = string.IsNullOrWhiteSpace(request.DecidedBy) ? "manager" : request.DecidedBy.Trim();
        entity.Status = decision == ApprovalDecision.Approve
            ? (int)ApprovalStatus.Approved
            : (int)ApprovalStatus.Rejected;
        entity.DecidedAt = DateTimeOffset.UtcNow;
        entity.DecidedBy = actor;
        entity.Notes = request.Notes;

        entity.History.Add(new ApprovalHistoryEntity
        {
            HistoryId = Guid.NewGuid(),
            ApprovalRequestId = entity.ApprovalRequestId,
            Action = decision.ToString(),
            Actor = actor,
            Notes = request.Notes,
            Timestamp = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Approval Received. Decision={Decision}, ApprovalRequestId={ApprovalRequestId}, WorkflowInstanceId={WorkflowInstanceId}, Actor={Actor}",
            decision,
            entity.ApprovalRequestId,
            entity.WorkflowInstanceId,
            actor);

        return ToResponse(entity);
    }

    private static ApprovalResponse ToResponse(ApprovalRequestEntity entity) =>
        new()
        {
            ApprovalRequestId = entity.ApprovalRequestId,
            WorkflowInstanceId = entity.WorkflowInstanceId,
            Status = (ApprovalStatus)entity.Status,
            Title = entity.Title,
            Description = entity.Description,
            DecidedBy = entity.DecidedBy,
            Notes = entity.Notes,
            RequestedAt = entity.RequestedAt,
            DecidedAt = entity.DecidedAt,
            Payload = string.IsNullOrWhiteSpace(entity.PayloadJson)
                ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(entity.PayloadJson, JsonOptions)
                  ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        };
}

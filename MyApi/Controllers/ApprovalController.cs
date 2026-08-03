using MyApi.AI.AGUI;
using MyApi.AI.Checkpointing;
using MyApi.AI.HITL;
using MyApi.AI.Streaming;
using MyApi.AI.Workflows.Engine;
using Microsoft.AspNetCore.Mvc;

namespace MyApi.Controllers;

/// <summary>
/// Human-in-the-loop approval endpoints. Approve/reject does not keep HTTP open while waiting —
/// resume loads the SQL checkpoint and continues from the next step.
/// </summary>
[ApiController]
[Route("api")]
public sealed class ApprovalController : ControllerBase
{
    private readonly IApprovalService _approvalService;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowResumeService _resumeService;
    private readonly IStreamingService _streaming;
    private readonly ILogger<ApprovalController> _logger;

    public ApprovalController(
        IApprovalService approvalService,
        IWorkflowEngine workflowEngine,
        IWorkflowResumeService resumeService,
        IStreamingService streaming,
        ILogger<ApprovalController> logger)
    {
        _approvalService = approvalService;
        _workflowEngine = workflowEngine;
        _resumeService = resumeService;
        _streaming = streaming;
        _logger = logger;
    }

    /// <summary>POST /api/approval/{workflowId}/approve — workflowId is the workflow instance id.</summary>
    [HttpPost("approval/{workflowId:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
        Guid workflowId,
        [FromBody] ApprovalDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var decision = request ?? new ApprovalDecisionRequest();
            var approval = await _approvalService.ApproveAsync(workflowId, decision, cancellationToken);

            var streamCtx = new StreamingContext
            {
                CorrelationId = Guid.NewGuid().ToString("N"),
                SessionId = null,
                UserId = decision.DecidedBy
            };

            await _streaming.PublishAsync(
                streamCtx,
                new ApprovalGrantedEvent
                {
                    CorrelationId = streamCtx.CorrelationId,
                    ApprovalRequestId = approval.ApprovalRequestId,
                    WorkflowInstanceId = workflowId,
                    DecidedBy = approval.DecidedBy
                },
                cancellationToken);

            var result = await _workflowEngine.ResumeAsync(workflowId, cancellationToken);

            _logger.LogInformation(
                "Approval granted and workflow resumed. InstanceId={InstanceId}, Status={Status}",
                workflowId,
                result.Status);

            return Ok(new
            {
                approval,
                workflow = new
                {
                    result.WorkflowInstanceId,
                    result.WorkflowId,
                    result.WorkflowName,
                    status = result.Status.ToString(),
                    result.Output,
                    result.Succeeded,
                    result.Error,
                    durationMs = result.Duration.TotalMilliseconds
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>POST /api/approval/{workflowId}/reject</summary>
    [HttpPost("approval/{workflowId:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        Guid workflowId,
        [FromBody] ApprovalDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var decision = request ?? new ApprovalDecisionRequest();
            var approval = await _approvalService.RejectAsync(workflowId, decision, cancellationToken);

            await _workflowEngine.CancelAsync(workflowId, cancellationToken);

            var streamCtx = new StreamingContext
            {
                CorrelationId = Guid.NewGuid().ToString("N"),
                UserId = decision.DecidedBy
            };

            await _streaming.PublishAsync(
                streamCtx,
                new ApprovalRejectedEvent
                {
                    CorrelationId = streamCtx.CorrelationId,
                    ApprovalRequestId = approval.ApprovalRequestId,
                    WorkflowInstanceId = workflowId,
                    DecidedBy = approval.DecidedBy,
                    Notes = approval.Notes
                },
                cancellationToken);

            return Ok(new { approval, status = "Cancelled" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>GET /api/approval/pending</summary>
    [HttpGet("approval/pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Pending(
        [FromQuery] string? userId,
        CancellationToken cancellationToken)
    {
        var pending = await _approvalService.GetPendingAsync(userId, cancellationToken);
        return Ok(pending);
    }

    /// <summary>GET /api/workflows/{workflowId}/status</summary>
    [HttpGet("workflows/{workflowId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WorkflowStatus(
        Guid workflowId,
        CancellationToken cancellationToken)
    {
        var status = await _resumeService.GetStatusAsync(workflowId, cancellationToken);
        if (status is null)
        {
            return NotFound(new { error = $"No workflow instance '{workflowId}'." });
        }

        return Ok(status);
    }
}

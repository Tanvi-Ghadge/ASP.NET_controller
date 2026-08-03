using MyApi.AI.HITL;
using MyApi.AI.Workflows.Engine;
using MyApi.AI.Workflows.Models;

namespace MyApi.AI.Workflows.Steps;

/// <summary>
/// Creates a human approval request and signals the engine to pause (HITL).
/// Does not delete — that happens only after resume.
/// </summary>
public sealed class RequestDeleteApprovalStep : IWorkflowStep
{
    private readonly IApprovalService _approvalService;
    private readonly ILogger<RequestDeleteApprovalStep> _logger;

    public RequestDeleteApprovalStep(
        IApprovalService approvalService,
        ILogger<RequestDeleteApprovalStep> logger)
    {
        _approvalService = approvalService;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "RequestDeleteApproval";

    /// <inheritdoc />
    public async Task<StepResult> ExecuteAsync(
        WorkflowExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var employeeId = context.State.Get<int?>(ValidateEmployeeDeleteStep.EmployeeIdKey);
        if (employeeId is null)
        {
            // Variables restored from JSON may be boxed as long/JsonElement-compatible numbers.
            if (context.State.Variables.TryGetValue(ValidateEmployeeDeleteStep.EmployeeIdKey, out var raw) &&
                raw is not null &&
                int.TryParse(raw.ToString(), out var parsed))
            {
                employeeId = parsed;
                context.State.Set(ValidateEmployeeDeleteStep.EmployeeIdKey, parsed);
            }
        }

        if (employeeId is null)
        {
            return StepResult.Failure("Employee id missing; cannot request delete approval.");
        }

        var title = $"Delete employee {employeeId}";
        var description =
            $"Manager approval required before permanently deleting employee {employeeId}.";

        var response = await _approvalService.RequestAsync(
            new ApprovalRequest
            {
                WorkflowInstanceId = context.State.InstanceId,
                WorkflowDefinitionId = context.State.WorkflowId,
                CorrelationId = context.CorrelationId,
                SessionId = context.Session.SessionId,
                UserId = context.UserId,
                Title = title,
                Description = description,
                Payload = new Dictionary<string, object?>
                {
                    ["employeeId"] = employeeId.Value,
                    ["operation"] = "DeleteEmployee"
                },
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            },
            cancellationToken);

        context.State.Set("ApprovalRequestId", response.ApprovalRequestId);
        context.State.Set("ApprovalTitle", title);

        _logger.LogInformation(
            "Approval Requested for delete. ApprovalRequestId={ApprovalRequestId}, EmployeeId={EmployeeId}, InstanceId={InstanceId}",
            response.ApprovalRequestId,
            employeeId,
            context.State.InstanceId);

        var message =
            $"Approval Required. WorkflowInstanceId={context.State.InstanceId}. " +
            $"ApprovalRequestId={response.ApprovalRequestId}. " +
            $"Awaiting manager approval to delete employee {employeeId}.";

        return StepResult.WaitingForApproval(message, response.ApprovalRequestId);
    }
}

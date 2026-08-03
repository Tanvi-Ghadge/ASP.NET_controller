namespace MyApi.AI.Models;

/// <summary>
/// Outgoing chat payload returned by POST /api/chat.
/// </summary>
public sealed class ChatResponse
{
    /// <summary>Conversation id to send on subsequent turns.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Assistant reply text.</summary>
    public string Response { get; set; } = string.Empty;

    /// <summary>True when human approval is required before the workflow continues.</summary>
    public bool ApprovalRequired { get; set; }

    /// <summary>Workflow instance id used with /api/approval/{workflowId}/approve.</summary>
    public Guid? WorkflowInstanceId { get; set; }

    /// <summary>Pending approval request id.</summary>
    public Guid? ApprovalRequestId { get; set; }

    /// <summary>Coarse status string (e.g. Completed, ApprovalRequired).</summary>
    public string Status { get; set; } = "Completed";

    public ChatResponse()
    {
    }

    public ChatResponse(string sessionId, string response)
    {
        SessionId = sessionId;
        Response = response;
    }
}

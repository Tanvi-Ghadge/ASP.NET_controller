using System.Text.Json.Serialization;

namespace MyApi.AI.AGUI;

/// <summary>
/// Base type for all AG-UI execution events. Prefer concrete subclasses over object bags.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ExecutionStartedEvent), "execution.started")]
[JsonDerivedType(typeof(ExecutionCompletedEvent), "execution.completed")]
[JsonDerivedType(typeof(ErrorEvent), "error")]
[JsonDerivedType(typeof(SessionLoadedEvent), "session.loaded")]
[JsonDerivedType(typeof(MemoryLoadedEvent), "memory.loaded")]
[JsonDerivedType(typeof(PlanningEvent), "planning")]
[JsonDerivedType(typeof(WorkflowStartedEvent), "workflow.started")]
[JsonDerivedType(typeof(WorkflowCompletedEvent), "workflow.completed")]
[JsonDerivedType(typeof(WorkflowStepStartedEvent), "workflow.step.started")]
[JsonDerivedType(typeof(WorkflowStepCompletedEvent), "workflow.step.completed")]
[JsonDerivedType(typeof(WorkflowStepFailedEvent), "workflow.step.failed")]
[JsonDerivedType(typeof(WorkflowStepSkippedEvent), "workflow.step.skipped")]
[JsonDerivedType(typeof(AgentSelectedEvent), "agent.selected")]
[JsonDerivedType(typeof(AgentStartedEvent), "agent.started")]
[JsonDerivedType(typeof(AgentCompletedEvent), "agent.completed")]
[JsonDerivedType(typeof(ToolStartedEvent), "tool.started")]
[JsonDerivedType(typeof(ToolCompletedEvent), "tool.completed")]
[JsonDerivedType(typeof(PluginInvokedEvent), "plugin.invoked")]
[JsonDerivedType(typeof(DelegationStartedEvent), "delegation.started")]
[JsonDerivedType(typeof(DelegationFinishedEvent), "delegation.finished")]
[JsonDerivedType(typeof(StreamingResponseStartedEvent), "stream.started")]
[JsonDerivedType(typeof(TokenStreamEvent), "stream.token")]
[JsonDerivedType(typeof(StreamingCompletedEvent), "stream.completed")]
[JsonDerivedType(typeof(WorkflowPausedEvent), "workflow.paused")]
[JsonDerivedType(typeof(WorkflowResumedEvent), "workflow.resumed")]
[JsonDerivedType(typeof(ApprovalRequestedEvent), "approval.requested")]
[JsonDerivedType(typeof(ApprovalGrantedEvent), "approval.granted")]
[JsonDerivedType(typeof(ApprovalRejectedEvent), "approval.rejected")]
[JsonDerivedType(typeof(CheckpointSavedEvent), "checkpoint.saved")]
[JsonDerivedType(typeof(CheckpointLoadedEvent), "checkpoint.loaded")]
[JsonDerivedType(typeof(TelemetrySnapshotEvent), "telemetry.snapshot")]
public abstract class AgentUiEvent
{
    /// <summary>Correlation id shared across the execution.</summary>
    public required string CorrelationId { get; init; }

    /// <summary>UTC event time.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Execution has been accepted by the harness.</summary>
public sealed class ExecutionStartedEvent : AgentUiEvent
{
    public string? SessionId { get; init; }
    public string? UserId { get; init; }
    public required string MessagePreview { get; init; }
}

/// <summary>Execution finished successfully or with a handled failure.</summary>
public sealed class ExecutionCompletedEvent : AgentUiEvent
{
    public string? SessionId { get; init; }
    public required bool Succeeded { get; init; }
    public string? FinalResponsePreview { get; init; }
    public long DurationMs { get; init; }
}

/// <summary>Unhandled or step-level error.</summary>
public sealed class ErrorEvent : AgentUiEvent
{
    public required string Message { get; init; }
    public string? Source { get; init; }
}

/// <summary>Session was loaded or created.</summary>
public sealed class SessionLoadedEvent : AgentUiEvent
{
    public required string SessionId { get; init; }
    public bool IsNew { get; init; }
    public int MessageCount { get; init; }
}

/// <summary>Long-term memory was retrieved.</summary>
public sealed class MemoryLoadedEvent : AgentUiEvent
{
    public required string UserId { get; init; }
    public int MemoryCount { get; init; }
}

/// <summary>Orchestrator planning stage.</summary>
public sealed class PlanningEvent : AgentUiEvent
{
    public required string Phase { get; init; }
    public string? Intent { get; init; }
    public string? WorkflowId { get; init; }
    public string? AgentKey { get; init; }
    public string? Rationale { get; init; }
}

/// <summary>Workflow started.</summary>
public sealed class WorkflowStartedEvent : AgentUiEvent
{
    public required string WorkflowId { get; init; }
    public required string WorkflowName { get; init; }
    public int StepCount { get; init; }
}

/// <summary>Workflow completed.</summary>
public sealed class WorkflowCompletedEvent : AgentUiEvent
{
    public required string WorkflowId { get; init; }
    public required string WorkflowName { get; init; }
    public required string Status { get; init; }
    public long DurationMs { get; init; }
}

/// <summary>Workflow step started.</summary>
public sealed class WorkflowStepStartedEvent : AgentUiEvent
{
    public required string WorkflowId { get; init; }
    public required string StepName { get; init; }
    public int StepIndex { get; init; }
}

/// <summary>Workflow step completed.</summary>
public sealed class WorkflowStepCompletedEvent : AgentUiEvent
{
    public required string WorkflowId { get; init; }
    public required string StepName { get; init; }
    public long DurationMs { get; init; }
    public string? Message { get; init; }
}

/// <summary>Workflow step failed.</summary>
public sealed class WorkflowStepFailedEvent : AgentUiEvent
{
    public required string WorkflowId { get; init; }
    public required string StepName { get; init; }
    public required string Error { get; init; }
    public long DurationMs { get; init; }
}

/// <summary>Workflow step skipped.</summary>
public sealed class WorkflowStepSkippedEvent : AgentUiEvent
{
    public required string WorkflowId { get; init; }
    public required string StepName { get; init; }
    public string? Reason { get; init; }
}

/// <summary>Agent was selected by the orchestrator / router.</summary>
public sealed class AgentSelectedEvent : AgentUiEvent
{
    public required string AgentKey { get; init; }
    public required string AgentName { get; init; }
    public string? Domain { get; init; }
}

/// <summary>Agent started reasoning or A2A handling.</summary>
public sealed class AgentStartedEvent : AgentUiEvent
{
    public required string AgentKey { get; init; }
    public required string AgentName { get; init; }
    public string? Mode { get; init; }
}

/// <summary>Agent finished.</summary>
public sealed class AgentCompletedEvent : AgentUiEvent
{
    public required string AgentKey { get; init; }
    public required string AgentName { get; init; }
    public long DurationMs { get; init; }
    public bool Succeeded { get; init; } = true;
}

/// <summary>Tool invocation started.</summary>
public sealed class ToolStartedEvent : AgentUiEvent
{
    public required string ToolName { get; init; }
    public string? AgentName { get; init; }
}

/// <summary>Tool invocation completed.</summary>
public sealed class ToolCompletedEvent : AgentUiEvent
{
    public required string ToolName { get; init; }
    public string? AgentName { get; init; }
    public long DurationMs { get; init; }
    public bool Succeeded { get; init; } = true;
}

/// <summary>Plugin method invoked.</summary>
public sealed class PluginInvokedEvent : AgentUiEvent
{
    public required string PluginName { get; init; }
    public required string Operation { get; init; }
}

/// <summary>A2A delegation started.</summary>
public sealed class DelegationStartedEvent : AgentUiEvent
{
    public required string TargetAgentKey { get; init; }
    public required string Operation { get; init; }
}

/// <summary>A2A delegation finished.</summary>
public sealed class DelegationFinishedEvent : AgentUiEvent
{
    public required string TargetAgentKey { get; init; }
    public required string Operation { get; init; }
    public required string Status { get; init; }
    public long DurationMs { get; init; }
}

/// <summary>Token streaming of the assistant response has begun.</summary>
public sealed class StreamingResponseStartedEvent : AgentUiEvent
{
    public string? AgentName { get; init; }
}

/// <summary>A partial token / text chunk.</summary>
public sealed class TokenStreamEvent : AgentUiEvent
{
    public required string Token { get; init; }
    public required string AccumulatedText { get; init; }
    public int TokenIndex { get; init; }
}

/// <summary>Token streaming finished.</summary>
public sealed class StreamingCompletedEvent : AgentUiEvent
{
    public required string FullText { get; init; }
    public int TokenCount { get; init; }
}

/// <summary>Workflow paused (typically waiting for human approval).</summary>
public sealed class WorkflowPausedEvent : AgentUiEvent
{
    public required Guid WorkflowInstanceId { get; init; }
    public required string WorkflowId { get; init; }
    public required string Reason { get; init; }
    public int ResumeStepIndex { get; init; }
}

/// <summary>Workflow resumed from a durable checkpoint.</summary>
public sealed class WorkflowResumedEvent : AgentUiEvent
{
    public required Guid WorkflowInstanceId { get; init; }
    public required string WorkflowId { get; init; }
    public int ResumeStepIndex { get; init; }
}

/// <summary>Human approval was requested.</summary>
public sealed class ApprovalRequestedEvent : AgentUiEvent
{
    public required Guid ApprovalRequestId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public required string Title { get; init; }
}

/// <summary>Human approved the request.</summary>
public sealed class ApprovalGrantedEvent : AgentUiEvent
{
    public required Guid ApprovalRequestId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public string? DecidedBy { get; init; }
}

/// <summary>Human rejected the request.</summary>
public sealed class ApprovalRejectedEvent : AgentUiEvent
{
    public required Guid ApprovalRequestId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public string? DecidedBy { get; init; }
    public string? Notes { get; init; }
}

/// <summary>Checkpoint persisted to SQL.</summary>
public sealed class CheckpointSavedEvent : AgentUiEvent
{
    public required Guid CheckpointId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public int ResumeStepIndex { get; init; }
}

/// <summary>Checkpoint loaded from SQL for resume.</summary>
public sealed class CheckpointLoadedEvent : AgentUiEvent
{
    public required Guid CheckpointId { get; init; }
    public required Guid WorkflowInstanceId { get; init; }
    public int ResumeStepIndex { get; init; }
}

/// <summary>End-of-execution telemetry snapshot for AG-UI dashboards.</summary>
public sealed class TelemetrySnapshotEvent : AgentUiEvent
{
    public required string ExecutionId { get; init; }
    public string? WorkflowId { get; init; }
    public string? AgentName { get; init; }
    public long DurationMs { get; init; }
    public int ToolCount { get; init; }
    public decimal EstimatedCostUsd { get; init; }
    public IReadOnlyList<string> Timeline { get; init; } = Array.Empty<string>();
}

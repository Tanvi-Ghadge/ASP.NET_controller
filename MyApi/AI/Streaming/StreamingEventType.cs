namespace MyApi.AI.Streaming;

/// <summary>
/// Transport-level classification for AG-UI / streaming envelopes.
/// </summary>
public enum StreamingEventType
{
    ExecutionStarted = 0,
    ExecutionFinished = 1,
    Error = 2,
    SessionLoaded = 3,
    MemoryRetrieved = 4,
    PlanningStarted = 5,
    PlanningCompleted = 6,
    WorkflowSelected = 7,
    WorkflowStarted = 8,
    WorkflowCompleted = 9,
    StepStarted = 10,
    StepCompleted = 11,
    StepFailed = 12,
    StepSkipped = 13,
    AgentSelected = 14,
    AgentStarted = 15,
    AgentCompleted = 16,
    ToolStarted = 17,
    ToolCompleted = 18,
    PluginInvoked = 19,
    DelegationStarted = 20,
    DelegationFinished = 21,
    StreamingResponseStarted = 22,
    StreamingToken = 23,
    StreamingCompleted = 24,
    WorkflowPaused = 25,
    WorkflowResumed = 26,
    ApprovalRequested = 27,
    ApprovalGranted = 28,
    ApprovalRejected = 29,
    CheckpointSaved = 30,
    CheckpointLoaded = 31,
    TelemetrySnapshot = 32
}

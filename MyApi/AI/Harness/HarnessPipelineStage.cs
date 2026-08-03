namespace MyApi.AI.Harness;

/// <summary>
/// Ordered stages of a harness execution. Enables telemetry, future middleware,
/// checkpointing, and human-approval hooks between stages.
/// </summary>
public enum HarnessPipelineStage
{
    ReceiveRequest = 0,
    LoadSession = 1,
    LoadMemory = 2,
    BuildContext = 3,
    PreparePrompt = 4,
    InvokeAgent = 5,
    ReceiveAgentResult = 6,
    UpdateSession = 7,
    ExtractMemories = 8,
    PersistMemories = 9,
    Complete = 10
}

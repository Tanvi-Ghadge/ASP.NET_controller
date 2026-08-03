using System.Diagnostics;
using Microsoft.Extensions.AI;
using MyApi.AI.AGUI;
using MyApi.AI.Memory;
using MyApi.AI.Middleware;
using MyApi.AI.Observability;
using MyApi.AI.Orchestration;
using MyApi.AI.Sessions;
using MyApi.AI.Streaming;

namespace MyApi.AI.Harness;

/// <summary>
/// Employee-domain harness: session/memory lifecycle + middleware pipeline + orchestration + AG-UI events.
/// Controllers never publish events — the harness owns the execution timeline.
/// Harness never invokes middleware manually; it uses <see cref="IAgentMiddlewarePipeline"/>.
/// </summary>
public sealed class EmployeeHarness : IAgentHarness
{
    public const string DefaultUserId = "default-user";

    private readonly ISessionStore _sessionStore;
    private readonly IMemoryStore _memoryStore;
    private readonly IMemoryRetrievalService _memoryRetrieval;
    private readonly IMemoryExtractionService _memoryExtraction;
    private readonly IHarnessContextFactory _contextFactory;
    private readonly IOrchestrator _orchestrator;
    private readonly IAgentMiddlewarePipeline _middlewarePipeline;
    private readonly IStreamingService _streaming;
    private readonly ILogger<EmployeeHarness> _logger;

    public EmployeeHarness(
        ISessionStore sessionStore,
        IMemoryStore memoryStore,
        IMemoryRetrievalService memoryRetrieval,
        IMemoryExtractionService memoryExtraction,
        IHarnessContextFactory contextFactory,
        IOrchestrator orchestrator,
        IAgentMiddlewarePipeline middlewarePipeline,
        IStreamingService streaming,
        ILogger<EmployeeHarness> logger)
    {
        _sessionStore = sessionStore;
        _memoryStore = memoryStore;
        _memoryRetrieval = memoryRetrieval;
        _memoryExtraction = memoryExtraction;
        _contextFactory = contextFactory;
        _orchestrator = orchestrator;
        _middlewarePipeline = middlewarePipeline;
        _streaming = streaming;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<HarnessExecutionResult> ExecuteAsync(
        string? sessionId,
        string? userId,
        string message,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(sessionId, userId, message, streamingContext: null, cancellationToken);

    /// <inheritdoc />
    public async Task<HarnessExecutionResult> ExecuteAsync(
        string? sessionId,
        string? userId,
        string message,
        StreamingContext? streamingContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        var correlationId = streamingContext?.CorrelationId ?? Guid.NewGuid().ToString("N");
        var pipeline = new HarnessExecutionPipeline(_logger, correlationId);
        var totalSw = Stopwatch.StartNew();
        var resolvedUserId = string.IsNullOrWhiteSpace(userId) ? DefaultUserId : userId.Trim();

        var streamCtx = streamingContext ?? new StreamingContext
        {
            CorrelationId = correlationId,
            SessionId = sessionId,
            UserId = resolvedUserId
        };
        streamCtx.UserId = resolvedUserId;

        using var registration = _streaming.RegisterExecution(correlationId);
        using var scope = _streaming.BeginScope(streamCtx);

        await _streaming.PublishAsync(
            streamCtx,
            new ExecutionStartedEvent
            {
                CorrelationId = correlationId,
                SessionId = sessionId,
                UserId = resolvedUserId,
                MessagePreview = message.Length <= 120 ? message : message[..120] + "…"
            },
            cancellationToken);

        await pipeline.RunAsync(
            HarnessPipelineStage.ReceiveRequest,
            _ =>
            {
                _logger.LogInformation(
                    "Harness execution started. CorrelationId={CorrelationId}, UserId={UserId}, SessionId={SessionId}, MemoryStore={MemoryStore}",
                    correlationId,
                    resolvedUserId,
                    string.IsNullOrWhiteSpace(sessionId) ? "(new)" : sessionId,
                    _memoryStore.GetType().Name);
                return Task.CompletedTask;
            },
            cancellationToken,
            new { resolvedUserId, sessionId });

        try
        {
            var session = await pipeline.RunAsync(
                HarnessPipelineStage.LoadSession,
                ct => ResolveSessionAsync(sessionId, ct),
                cancellationToken,
                new { requestedSessionId = sessionId });

            streamCtx.SessionId = session.SessionId;
            await _streaming.PublishAsync(
                streamCtx,
                new SessionLoadedEvent
                {
                    CorrelationId = correlationId,
                    SessionId = session.SessionId,
                    IsNew = string.IsNullOrWhiteSpace(sessionId) ||
                            !string.Equals(sessionId, session.SessionId, StringComparison.Ordinal),
                    MessageCount = session.Messages.Count
                },
                cancellationToken);

            var memory = await pipeline.RunAsync(
                HarnessPipelineStage.LoadMemory,
                ct => _memoryRetrieval.RetrieveAsync(resolvedUserId, message, ct),
                cancellationToken,
                new { resolvedUserId });

            await _streaming.PublishAsync(
                streamCtx,
                new MemoryLoadedEvent
                {
                    CorrelationId = correlationId,
                    UserId = resolvedUserId,
                    MemoryCount = memory.Memories.Count
                },
                cancellationToken);

            _logger.LogInformation(
                "Memory retrieved. CorrelationId={CorrelationId}, UserId={UserId}, Count={MemoryCount}",
                correlationId,
                resolvedUserId,
                memory.Memories.Count);

            var context = await pipeline.RunAsync(
                HarnessPipelineStage.BuildContext,
                _ => Task.FromResult(_contextFactory.Create(
                    resolvedUserId,
                    session,
                    message,
                    memory,
                    correlationId,
                    cancellationToken)),
                cancellationToken);

            await pipeline.RunAsync(
                HarnessPipelineStage.PreparePrompt,
                _ =>
                {
                    context.PreparedMessages = BuildPreparedMessages(context);
                    _logger.LogInformation(
                        "Prompt prepared. CorrelationId={CorrelationId}, MessageCount={MessageCount}",
                        correlationId,
                        context.PreparedMessages.Count);
                    return Task.CompletedTask;
                },
                cancellationToken);

            await _streaming.PublishAsync(
                streamCtx,
                new PlanningEvent
                {
                    CorrelationId = correlationId,
                    Phase = "started",
                    Rationale = "Orchestrator classifying intent and selecting workflow/agent."
                },
                cancellationToken);

            var executionId = Guid.NewGuid().ToString("N");
            var agentContext = new AgentExecutionContext
            {
                ExecutionId = executionId,
                CorrelationId = correlationId,
                UserId = resolvedUserId,
                SessionId = session.SessionId,
                UserMessage = message,
                Harness = context,
                Streaming = streamCtx,
                CancellationToken = cancellationToken
            };
            agentContext.Telemetry.SessionLoaded = true;
            agentContext.Telemetry.MemoryRetrieved = memory.Memories.Count;

            OrchestrationResult? orchestrationResult = null;

            using (agentContext.BeginAmbientScope())
            using (AiActivitySource.StartActivity("ai.harness"))
            {
                await pipeline.RunAsync(
                    HarnessPipelineStage.InvokeAgent,
                    async ct =>
                    {
                        await _middlewarePipeline.ExecuteAsync(
                            agentContext,
                            async mwCtx =>
                            {
                                var orchestrationContext = new OrchestrationContext
                                {
                                    Harness = context,
                                    UserMessage = message,
                                    UserId = resolvedUserId,
                                    CorrelationId = correlationId,
                                    CancellationToken = ct
                                };
                                mwCtx.Orchestration = orchestrationContext;

                                var result = await _orchestrator.OrchestrateAsync(orchestrationContext, ct);
                                mwCtx.OrchestrationResult = result;
                                mwCtx.FinalResponse = result.ApprovalPending
                                    ? result.Output
                                    : (result.Succeeded ? result.Output : result.Error);
                                orchestrationResult = result;
                            },
                            ct);
                    },
                    cancellationToken);
            }

            if (orchestrationResult is null)
            {
                throw new InvalidOperationException("Middleware pipeline did not produce an orchestration result.");
            }

            // Prefer response-filter output when present.
            if (!string.IsNullOrWhiteSpace(agentContext.FilteredResponse))
            {
                orchestrationResult = new OrchestrationResult
                {
                    Plan = orchestrationResult.Plan,
                    Output = agentContext.FilteredResponse,
                    Succeeded = orchestrationResult.Succeeded,
                    ApprovalPending = orchestrationResult.ApprovalPending,
                    WorkflowInstanceId = orchestrationResult.WorkflowInstanceId,
                    ApprovalRequestId = orchestrationResult.ApprovalRequestId,
                    Error = orchestrationResult.Error,
                    Duration = orchestrationResult.Duration,
                    CorrelationId = orchestrationResult.CorrelationId,
                    Data = orchestrationResult.Data
                };
            }

            await _streaming.PublishAsync(
                streamCtx,
                new PlanningEvent
                {
                    CorrelationId = correlationId,
                    Phase = "completed",
                    Intent = orchestrationResult.Plan.DetectedIntent.ToString(),
                    WorkflowId = orchestrationResult.Plan.SelectedWorkflowId,
                    AgentKey = orchestrationResult.Plan.SelectedAgentKey,
                    Rationale = orchestrationResult.Plan.Rationale
                },
                cancellationToken);

            var assistantReply = orchestrationResult.ApprovalPending
                ? orchestrationResult.Output
                : (orchestrationResult.Succeeded
                    ? orchestrationResult.Output
                    : (orchestrationResult.Error ?? "Orchestration failed."));

            // Structured workflows produce a final string without LLM tokens — synthesize a stream for UX.
            var tokensAlreadyStreamed = context.Metadata.TryGetValue("TokensStreamed", out var streamedFlag)
                && streamedFlag is true;

            if ((orchestrationResult.Succeeded || orchestrationResult.ApprovalPending) &&
                !string.IsNullOrWhiteSpace(assistantReply) &&
                !tokensAlreadyStreamed &&
                ShouldSynthesizeTokenStream(orchestrationResult))
            {
                await _streaming.StreamTextAsTokensAsync(
                    streamCtx,
                    assistantReply,
                    orchestrationResult.Plan.SelectedAgentKey,
                    cancellationToken);
            }

            await pipeline.RunAsync(
                HarnessPipelineStage.ReceiveAgentResult,
                _ =>
                {
                    _logger.LogInformation(
                        "Orchestration result received. PlanId={PlanId}, Intent={Intent}, WorkflowId={WorkflowId}, AgentKey={AgentKey}, Succeeded={Succeeded}, ApprovalPending={ApprovalPending}, ResponseLength={Length}, CorrelationId={CorrelationId}",
                        orchestrationResult.Plan.PlanId,
                        orchestrationResult.Plan.DetectedIntent,
                        orchestrationResult.Plan.SelectedWorkflowId,
                        orchestrationResult.Plan.SelectedAgentKey,
                        orchestrationResult.Succeeded,
                        orchestrationResult.ApprovalPending,
                        assistantReply?.Length ?? 0,
                        correlationId);
                    return Task.CompletedTask;
                },
                cancellationToken);

            if (orchestrationResult.ApprovalPending)
            {
                totalSw.Stop();

                await pipeline.RunAsync(
                    HarnessPipelineStage.UpdateSession,
                    async ct =>
                    {
                        session.AddUserMessage(message);
                        session.AddAssistantMessage(assistantReply ?? "Approval Required");
                        await _sessionStore.UpdateAsync(session, ct);
                    },
                    cancellationToken,
                    new { session.SessionId });

                await _streaming.PublishAsync(
                    streamCtx,
                    new ExecutionCompletedEvent
                    {
                        CorrelationId = correlationId,
                        SessionId = session.SessionId,
                        Succeeded = true,
                        FinalResponsePreview = assistantReply,
                        DurationMs = totalSw.ElapsedMilliseconds
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "Harness returning Approval Pending. WorkflowInstanceId={InstanceId}, ApprovalRequestId={ApprovalRequestId}, CorrelationId={CorrelationId}",
                    orchestrationResult.WorkflowInstanceId,
                    orchestrationResult.ApprovalRequestId,
                    correlationId);

                return new HarnessExecutionResult
                {
                    SessionId = session.SessionId,
                    Response = assistantReply ?? "Approval Required",
                    CorrelationId = correlationId,
                    UserId = resolvedUserId,
                    MemoriesRetrieved = memory.Memories.Count,
                    Duration = totalSw.Elapsed,
                    Succeeded = true,
                    ApprovalPending = true,
                    WorkflowInstanceId = orchestrationResult.WorkflowInstanceId,
                    ApprovalRequestId = orchestrationResult.ApprovalRequestId
                };
            }

            if (!orchestrationResult.Succeeded)
            {
                totalSw.Stop();
                await _streaming.PublishAsync(
                    streamCtx,
                    new ErrorEvent
                    {
                        CorrelationId = correlationId,
                        Message = orchestrationResult.Error ?? "Orchestration failed.",
                        Source = "Orchestrator"
                    },
                    cancellationToken);

                await _streaming.PublishAsync(
                    streamCtx,
                    new ExecutionCompletedEvent
                    {
                        CorrelationId = correlationId,
                        SessionId = session.SessionId,
                        Succeeded = false,
                        FinalResponsePreview = assistantReply,
                        DurationMs = totalSw.ElapsedMilliseconds
                    },
                    cancellationToken);

                return new HarnessExecutionResult
                {
                    SessionId = session.SessionId,
                    Response = assistantReply ?? string.Empty,
                    CorrelationId = correlationId,
                    UserId = resolvedUserId,
                    MemoriesRetrieved = memory.Memories.Count,
                    Duration = totalSw.Elapsed,
                    Succeeded = false,
                    Error = orchestrationResult.Error
                };
            }

            await pipeline.RunAsync(
                HarnessPipelineStage.UpdateSession,
                async ct =>
                {
                    session.AddUserMessage(message);
                    session.AddAssistantMessage(assistantReply ?? string.Empty);
                    await _sessionStore.UpdateAsync(session, ct);
                },
                cancellationToken,
                new { session.SessionId });

            var extracted = await pipeline.RunAsync(
                HarnessPipelineStage.ExtractMemories,
                ct => _memoryExtraction.ExtractAsync(resolvedUserId, message, assistantReply, ct),
                cancellationToken);

            await pipeline.RunAsync(
                HarnessPipelineStage.PersistMemories,
                async ct =>
                {
                    foreach (var candidate in extracted)
                    {
                        await _memoryStore.SaveMemoryAsync(candidate, ct);
                    }

                    if (extracted.Count > 0)
                    {
                        _logger.LogInformation(
                            "Persisted {Count} memories. CorrelationId={CorrelationId}, UserId={UserId}",
                            extracted.Count,
                            correlationId,
                            resolvedUserId);
                    }
                },
                cancellationToken,
                new { extracted.Count });

            totalSw.Stop();

            await pipeline.RunAsync(
                HarnessPipelineStage.Complete,
                _ =>
                {
                    _logger.LogInformation(
                        "Harness execution finished. CorrelationId={CorrelationId}, SessionId={SessionId}, UserId={UserId}, DurationMs={DurationMs}, MemoriesRetrieved={MemoriesRetrieved}, WorkflowId={WorkflowId}, Intent={Intent}",
                        correlationId,
                        session.SessionId,
                        resolvedUserId,
                        totalSw.ElapsedMilliseconds,
                        memory.Memories.Count,
                        orchestrationResult.Plan.SelectedWorkflowId,
                        orchestrationResult.Plan.DetectedIntent);
                    return Task.CompletedTask;
                },
                cancellationToken);

            await _streaming.PublishAsync(
                streamCtx,
                new ExecutionCompletedEvent
                {
                    CorrelationId = correlationId,
                    SessionId = session.SessionId,
                    Succeeded = true,
                    FinalResponsePreview = assistantReply is { Length: > 200 }
                        ? assistantReply[..200] + "…"
                        : assistantReply,
                    DurationMs = totalSw.ElapsedMilliseconds
                },
                cancellationToken);

            return new HarnessExecutionResult
            {
                SessionId = session.SessionId,
                Response = assistantReply ?? string.Empty,
                CorrelationId = correlationId,
                UserId = resolvedUserId,
                MemoriesRetrieved = memory.Memories.Count,
                Duration = totalSw.Elapsed,
                Succeeded = true
            };
        }
        catch (Exception ex)
        {
            totalSw.Stop();
            _logger.LogError(
                ex,
                "Harness execution failed. CorrelationId={CorrelationId}, UserId={UserId}, DurationMs={DurationMs}",
                correlationId,
                resolvedUserId,
                totalSw.ElapsedMilliseconds);

            await _streaming.PublishAsync(
                streamCtx,
                new ErrorEvent
                {
                    CorrelationId = correlationId,
                    Message = ex.Message,
                    Source = "Harness"
                },
                cancellationToken);

            await _streaming.PublishAsync(
                streamCtx,
                new ExecutionCompletedEvent
                {
                    CorrelationId = correlationId,
                    SessionId = streamCtx.SessionId,
                    Succeeded = false,
                    DurationMs = totalSw.ElapsedMilliseconds
                },
                cancellationToken);

            return new HarnessExecutionResult
            {
                SessionId = sessionId ?? string.Empty,
                Response = string.Empty,
                CorrelationId = correlationId,
                UserId = resolvedUserId,
                Duration = totalSw.Elapsed,
                Succeeded = false,
                Error = ex.Message
            };
        }
    }

    private static bool ShouldSynthesizeTokenStream(OrchestrationResult result)
    {
        // Conversational agent paths already stream via AgentStreamingRunner.
        // Multi-agent / structured workflows typically set FinalResponse without LLM streaming.
        var workflowId = result.Plan.SelectedWorkflowId ?? string.Empty;
        return workflowId.Contains("report", StringComparison.OrdinalIgnoreCase)
               || workflowId.Contains("payroll", StringComparison.OrdinalIgnoreCase)
               || workflowId.Contains("lookup", StringComparison.OrdinalIgnoreCase)
               || workflowId.Contains("delete", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<ChatMessage> BuildPreparedMessages(HarnessContext context)
    {
        var messages = new List<ChatMessage>();

        var memoryFragment = context.Memory.ToSystemPromptFragment();
        if (!string.IsNullOrWhiteSpace(memoryFragment))
        {
            messages.Add(new ChatMessage(ChatRole.System, memoryFragment));
        }

        foreach (var prior in context.ConversationHistory)
        {
            messages.Add(ToChatMessage(prior));
        }

        messages.Add(new ChatMessage(ChatRole.User, context.CurrentMessage));
        return messages;
    }

    private static ChatMessage ToChatMessage(SessionMessage message) =>
        message.Role switch
        {
            SessionMessageRole.Assistant => new ChatMessage(ChatRole.Assistant, message.Content),
            SessionMessageRole.System => new ChatMessage(ChatRole.System, message.Content),
            _ => new ChatMessage(ChatRole.User, message.Content)
        };

    private async Task<AgentSession> ResolveSessionAsync(
        string? sessionId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            var existing = await _sessionStore.GetAsync(sessionId, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            _logger.LogWarning(
                "Session {SessionId} was not found; creating a new session.",
                sessionId);
        }

        return await _sessionStore.CreateAsync(cancellationToken);
    }
}

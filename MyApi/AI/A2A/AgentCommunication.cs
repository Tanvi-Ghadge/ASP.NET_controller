using System.Diagnostics;
using MyApi.AI.AGUI;
using MyApi.AI.AgentRouter;
using MyApi.AI.Agents;
using MyApi.AI.Streaming;
using AgentResponse = MyApi.AI.A2A.AgentResponse;

namespace MyApi.AI.A2A;

/// <summary>
/// Default A2A bus: resolves the target via <see cref="IAgentRouter"/> and invokes <see cref="IDomainAgent.HandleAsync"/>.
/// Publishes Delegation Started / Finished AG-UI events.
/// </summary>
public sealed class AgentCommunication : IAgentCommunication
{
    private readonly Func<IAgentRouter> _routerFactory;
    private readonly IStreamingService _streaming;
    private readonly ILogger<AgentCommunication> _logger;

    public AgentCommunication(
        Func<IAgentRouter> routerFactory,
        IStreamingService streaming,
        ILogger<AgentCommunication> logger)
    {
        _routerFactory = routerFactory;
        _streaming = streaming;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AgentResponse> SendAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var streamCtx = _streaming.Current ?? new StreamingContext
        {
            CorrelationId = request.CorrelationId,
            SessionId = request.SessionId
        };

        await _streaming.PublishAsync(
            streamCtx,
            new DelegationStartedEvent
            {
                CorrelationId = request.CorrelationId,
                TargetAgentKey = request.AgentName,
                Operation = request.Operation
            },
            cancellationToken);

        _logger.LogInformation(
            "Agent Delegation. Target={TargetAgent}, Operation={Operation}, CorrelationId={CorrelationId}",
            request.AgentName,
            request.Operation,
            request.CorrelationId);

        var router = _routerFactory();
        IDomainAgent agent;
        try
        {
            agent = router.Resolve(request.AgentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Agent Receiving Request failed — target not resolved. Target={TargetAgent}, CorrelationId={CorrelationId}",
                request.AgentName,
                request.CorrelationId);

            await _streaming.PublishAsync(
                streamCtx,
                new DelegationFinishedEvent
                {
                    CorrelationId = request.CorrelationId,
                    TargetAgentKey = request.AgentName,
                    Operation = request.Operation,
                    Status = "Failed",
                    DurationMs = 0
                },
                cancellationToken);

            return AgentResponse.Failure(request.AgentName, ex.Message, TimeSpan.Zero);
        }

        _logger.LogInformation(
            "Agent Receiving Request. Agent={AgentName}, Operation={Operation}, CorrelationId={CorrelationId}",
            agent.AgentName,
            request.Operation,
            request.CorrelationId);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await agent.HandleAsync(request, cancellationToken);
            sw.Stop();

            await _streaming.PublishAsync(
                streamCtx,
                new DelegationFinishedEvent
                {
                    CorrelationId = request.CorrelationId,
                    TargetAgentKey = agent.AgentKey,
                    Operation = request.Operation,
                    Status = response.Status.ToString(),
                    DurationMs = sw.ElapsedMilliseconds
                },
                cancellationToken);

            _logger.LogInformation(
                "Agent Completed (A2A). Agent={AgentName}, Status={Status}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
                agent.AgentName,
                response.Status,
                sw.ElapsedMilliseconds,
                request.CorrelationId);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            await _streaming.PublishAsync(
                streamCtx,
                new DelegationFinishedEvent
                {
                    CorrelationId = request.CorrelationId,
                    TargetAgentKey = agent.AgentKey,
                    Operation = request.Operation,
                    Status = "Failed",
                    DurationMs = sw.ElapsedMilliseconds
                },
                cancellationToken);

            _logger.LogError(
                ex,
                "Agent Failed (A2A). Agent={AgentName}, DurationMs={DurationMs}, CorrelationId={CorrelationId}",
                agent.AgentName,
                sw.ElapsedMilliseconds,
                request.CorrelationId);
            return AgentResponse.Failure(agent.AgentName, ex.Message, sw.Elapsed);
        }
    }
}

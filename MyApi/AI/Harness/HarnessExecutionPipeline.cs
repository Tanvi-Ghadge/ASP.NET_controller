using System.Diagnostics;

namespace MyApi.AI.Harness;

/// <summary>
/// Lightweight execution pipeline that times and logs each harness stage.
/// Future workflow middleware, checkpoints, and human-approval gates can wrap these stages.
/// </summary>
public sealed class HarnessExecutionPipeline
{
    private readonly ILogger _logger;
    private readonly string _correlationId;

    public HarnessExecutionPipeline(ILogger logger, string correlationId)
    {
        _logger = logger;
        _correlationId = correlationId;
    }

    /// <summary>Runs a stage that returns a value and logs start/finish/duration.</summary>
    public async Task<T> RunAsync<T>(
        HarnessPipelineStage stage,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        object? details = null)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation(
            "Harness stage {Stage} started. CorrelationId={CorrelationId}. Details={@Details}",
            stage,
            _correlationId,
            details);

        try
        {
            var result = await action(cancellationToken);
            sw.Stop();
            _logger.LogInformation(
                "Harness stage {Stage} finished in {ElapsedMs} ms. CorrelationId={CorrelationId}",
                stage,
                sw.ElapsedMilliseconds,
                _correlationId);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Harness stage {Stage} failed after {ElapsedMs} ms. CorrelationId={CorrelationId}",
                stage,
                sw.ElapsedMilliseconds,
                _correlationId);
            throw;
        }
    }

    /// <summary>Runs a stage with no return value.</summary>
    public Task RunAsync(
        HarnessPipelineStage stage,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        object? details = null) =>
        RunAsync(stage, async ct =>
        {
            await action(ct);
            return true;
        }, cancellationToken, details);
}

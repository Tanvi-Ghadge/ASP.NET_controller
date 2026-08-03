using System.Diagnostics;
using MyApi.AI.Middleware;

namespace MyApi.AI.Observability;

/// <summary>
/// Runs plugin/tool work through BeforeTool / AfterTool middleware + OpenTelemetry spans.
/// </summary>
public interface IToolInvocationPipeline
{
    Task<T> InvokeAsync<T>(
        string pluginName,
        string toolName,
        object? parameters,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default tool invocation gate used by plugins.
/// </summary>
public sealed class ToolInvocationPipeline : IToolInvocationPipeline
{
    private readonly IAgentMiddlewarePipeline _pipeline;
    private readonly ILogger<ToolInvocationPipeline> _logger;

    public ToolInvocationPipeline(
        IAgentMiddlewarePipeline pipeline,
        ILogger<ToolInvocationPipeline> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<T> InvokeAsync<T>(
        string pluginName,
        string toolName,
        object? parameters,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var context = AgentExecutionContext.Current;
        using var activity = AiActivitySource.StartActivity($"ai.tool.{toolName}");
        activity?.SetTag("plugin.name", pluginName);
        activity?.SetTag("tool.name", toolName);

        if (context is null)
        {
            return await action();
        }

        context.CurrentPluginName = pluginName;
        context.CurrentToolName = toolName;
        context.CurrentToolParameters = parameters;
        context.CurrentToolSucceeded = true;
        context.CurrentToolError = null;

        var sw = Stopwatch.StartNew();
        try
        {
            await _pipeline.InvokeBeforeToolAsync(context, cancellationToken);

            if (context.RejectedReason is not null)
            {
                throw new UnauthorizedAccessException(context.RejectedReason);
            }

            var result = await action();
            context.CurrentToolResult = result is string s
                ? (s.Length <= 500 ? s : s[..500] + "…")
                : result;
            sw.Stop();

            context.Telemetry.Tools.Add(new ToolInvocationTelemetry
            {
                ToolName = toolName,
                PluginName = pluginName,
                DurationMs = sw.ElapsedMilliseconds,
                Succeeded = true,
                RetryCount = context.ToolRetryCount,
                ParametersPreview = parameters?.ToString(),
                ResultPreview = context.CurrentToolResult?.ToString()
            });

            await _pipeline.InvokeAfterToolAsync(context, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            context.CurrentToolSucceeded = false;
            context.CurrentToolError = ex.Message;
            context.Telemetry.Tools.Add(new ToolInvocationTelemetry
            {
                ToolName = toolName,
                PluginName = pluginName,
                DurationMs = sw.ElapsedMilliseconds,
                Succeeded = false,
                Error = ex.Message,
                RetryCount = context.ToolRetryCount,
                ParametersPreview = parameters?.ToString()
            });

            try
            {
                await _pipeline.InvokeAfterToolAsync(context, cancellationToken);
            }
            catch (Exception hookEx)
            {
                _logger.LogWarning(hookEx, "AfterTool middleware failed for {ToolName}", toolName);
            }

            throw;
        }
    }
}

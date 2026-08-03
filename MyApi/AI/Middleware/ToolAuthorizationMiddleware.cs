namespace MyApi.AI.Middleware;

/// <summary>
/// Authorizes individual tool invocations (BeforeTool / AfterTool).
/// </summary>
public sealed class ToolAuthorizationMiddleware : IAgentMiddleware
{
    private static readonly HashSet<string> RestrictedTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "DeleteEmployee",
        "CreateEmployee"
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ToolAuthorizationMiddleware> _logger;

    public ToolAuthorizationMiddleware(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ToolAuthorizationMiddleware> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string Name => "ToolAuthorization";
    public int Order => 60;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next) =>
        await next(context);

    public Task OnBeforeToolAsync(AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        var tool = context.CurrentToolName ?? string.Empty;
        var user = _httpContextAccessor.HttpContext?.User;

        if (RestrictedTools.Contains(tool) &&
            user?.Identity?.IsAuthenticated == true &&
            user.HasClaim("permission", "employee.write.deny"))
        {
            context.RejectedReason = $"Tool '{tool}' is not authorized for this user.";
            _logger.LogWarning(
                "Tool authorization denied. Tool={Tool}, ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, UserId={UserId}",
                tool,
                context.ExecutionId,
                context.CorrelationId,
                context.UserId);
            throw new UnauthorizedAccessException(context.RejectedReason);
        }

        _logger.LogInformation(
            "Tool authorization allowed. Tool={Tool}, Plugin={Plugin}, ExecutionId={ExecutionId}, CorrelationId={CorrelationId}",
            tool,
            context.CurrentPluginName,
            context.ExecutionId,
            context.CorrelationId);

        return Task.CompletedTask;
    }
}

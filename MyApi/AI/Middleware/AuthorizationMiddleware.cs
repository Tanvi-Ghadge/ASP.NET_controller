namespace MyApi.AI.Middleware;

/// <summary>
/// Verifies the user may run AI workflows (claim-based; extensible).
/// </summary>
public sealed class AuthorizationMiddleware : IAgentMiddleware
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthorizationMiddleware> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string Name => "Authorization";
    public int Order => 20;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true &&
            user.HasClaim("permission", "ai.deny"))
        {
            context.IsAuthorized = false;
            context.RejectedReason = "User is not authorized to execute AI workflows.";
            _logger.LogWarning(
                "Authorization rejected. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, UserId={UserId}",
                context.ExecutionId,
                context.CorrelationId,
                context.UserId);
            throw new UnauthorizedAccessException(context.RejectedReason);
        }

        context.IsAuthorized = true;
        context.Telemetry.Mark("Authorization.Passed");
        await next(context);
    }
}

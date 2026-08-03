using System.Security.Claims;

namespace MyApi.AI.Middleware;

/// <summary>
/// Verifies an authenticated principal exists (JWT or harness user id).
/// </summary>
public sealed class AuthenticationMiddleware : IAgentMiddleware
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthenticationMiddleware> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string Name => "Authentication";
    public int Order => 10;

    public async Task InvokeAsync(AgentExecutionContext context, AgentMiddlewareDelegate next)
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;

        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            context.UserId = httpUser.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? httpUser.FindFirstValue(ClaimTypes.Name)
                             ?? context.UserId;
            context.IsAuthenticated = true;
        }
        else if (!string.IsNullOrWhiteSpace(context.UserId))
        {
            context.IsAuthenticated = true;
        }
        else
        {
            context.IsAuthenticated = false;
            context.RejectedReason = "Unauthenticated AI execution.";
            _logger.LogWarning(
                "Authentication rejected. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}",
                context.ExecutionId,
                context.CorrelationId);
            throw new UnauthorizedAccessException(context.RejectedReason);
        }

        context.Telemetry.Mark("Authentication.Passed");
        _logger.LogInformation(
            "Authentication passed. ExecutionId={ExecutionId}, CorrelationId={CorrelationId}, UserId={UserId}",
            context.ExecutionId,
            context.CorrelationId,
            context.UserId);

        await next(context);
    }
}

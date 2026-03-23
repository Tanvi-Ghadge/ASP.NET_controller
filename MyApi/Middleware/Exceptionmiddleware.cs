using System;


namespace MyApi.Middleware;

public class Exceptionmiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<Exceptionmiddleware> _logger;
    public Exceptionmiddleware(RequestDelegate next, ILogger<Exceptionmiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var traceId = context.Items["TraceId"]?.ToString() ?? Guid.NewGuid().ToString();

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex, traceId);
        }
    }
    private async Task HandleException(HttpContext context, Exception ex, string traceId)
    {
        var (statusCode, message) = MapException(ex);

        // ✅ Logging with proper severity
        if (statusCode >= 500)
        {
            _logger.LogError(ex, "Server error. TraceId: {TraceId}", traceId);
        }
        else
        {
            _logger.LogWarning(ex, "Client error. TraceId: {TraceId}", traceId);
        }

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                message,
                traceId,
                statusCode
            });
        }
    }

    private (int statusCode, string message) MapException(Exception ex)
    {
        return ex switch
        {

            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),

            ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),

            KeyNotFoundException => (StatusCodes.Status404NotFound, ex.Message),

            InvalidOperationException => (StatusCodes.Status400BadRequest, ex.Message),

            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };
    }
}

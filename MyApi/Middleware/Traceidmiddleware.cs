using System;

namespace MyApi.Middleware;

public class Traceidmiddleware
{
    private readonly RequestDelegate _next;

    public Traceidmiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Read existing trace ID OR create new
        var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault()
                      ?? Guid.NewGuid().ToString();

        // 2. Store in HttpContext
        context.Items["TraceId"] = traceId;

        // 3. Add to response header
        context.Response.Headers["X-Trace-Id"] = traceId;

        // 4. Push into logging context
        using (Serilog.Context.LogContext.PushProperty("TraceId", traceId))
        {
            await _next(context);
        }
    }
}

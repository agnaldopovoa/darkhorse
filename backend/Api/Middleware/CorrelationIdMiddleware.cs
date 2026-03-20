using Serilog.Context;

namespace Darkhorse.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
            ?? context.TraceIdentifier;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.Headers.Append("X-Correlation-ID", correlationId);
            await next(context);
        }
    }
}

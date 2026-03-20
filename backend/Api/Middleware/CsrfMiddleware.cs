namespace Darkhorse.Api.Middleware;

public class CsrfMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            || HttpMethods.IsPut(context.Request.Method)
            || HttpMethods.IsDelete(context.Request.Method))
        {
            // Whitelist auth endpoints that don't need CSRF check (e.g. login itself sets the cookie)
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
                path.Contains("/api/auth/register", StringComparison.OrdinalIgnoreCase))
            {
                await next(context);
                return;
            }

            var cookieToken = context.Request.Cookies["csrf_token"];
            var headerToken = context.Request.Headers["X-CSRF-Token"].FirstOrDefault();

            if (string.IsNullOrEmpty(cookieToken) || cookieToken != headerToken)
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { detail = "CSRF validation failed" });
                return;
            }
        }
        await next(context);
    }
}

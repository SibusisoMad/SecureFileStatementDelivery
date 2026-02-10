namespace SecureFileStatementDelivery.Api.Middleware.Headers;

internal sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");
            context.Response.Headers.TryAdd("X-Permitted-Cross-Domain-Policies", "none");
            context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

            // Statement PDFs and metadata are sensitive; avoid caching by default.
            context.Response.Headers.TryAdd("Cache-Control", "no-store");
            context.Response.Headers.TryAdd("Pragma", "no-cache");

            if (context.Request.IsHttps)
            {
                context.Response.Headers.TryAdd("Strict-Transport-Security", "max-age=15552000");
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}

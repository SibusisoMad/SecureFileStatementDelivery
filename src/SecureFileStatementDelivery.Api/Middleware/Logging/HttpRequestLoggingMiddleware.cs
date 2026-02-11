using System.Diagnostics;

namespace SecureFileStatementDelivery.Api.Middleware.Logging;

internal sealed class HttpRequestLoggingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<HttpRequestLoggingMiddleware> logger)
    {
        using var _ = Log4NetContext.PushProperty("httpMethod", context.Request.Method);
        using var __ = Log4NetContext.PushProperty("httpPath", context.Request.Path.Value);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = context.Response.StatusCode;
            var elapsedMs = (long)stopwatch.Elapsed.TotalMilliseconds;

            using var ___ = Log4NetContext.PushProperty("statusCode", statusCode);
            using var ____ = Log4NetContext.PushProperty("elapsedMs", elapsedMs);

            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                elapsedMs);
        }
    }
}

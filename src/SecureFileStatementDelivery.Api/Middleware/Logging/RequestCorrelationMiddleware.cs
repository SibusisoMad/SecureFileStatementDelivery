using System.Diagnostics;

namespace SecureFileStatementDelivery.Api.Middleware.Logging;

internal sealed class RequestCorrelationMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetCorrelationId(context);

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using var _ = Log4NetContext.PushProperty("correlationId", correlationId);
        using var __ = Log4NetContext.PushProperty("traceId", correlationId);

        if (Activity.Current is not null)
        {
            Activity.Current.SetTag("correlationId", correlationId);
        }

        await next(context);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        var headerValue = context.Request.Headers[CorrelationIdHeaderName].ToString();
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.Trim();
        }

        return Guid.NewGuid().ToString("N");
    }
}

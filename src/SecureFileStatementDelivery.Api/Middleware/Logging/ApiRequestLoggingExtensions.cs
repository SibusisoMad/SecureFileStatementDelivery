namespace SecureFileStatementDelivery.Api.Middleware.Logging;

internal static class ApiRequestLoggingExtensions
{
    public static WebApplication UseApiRequestLogging(this WebApplication app)
    {
        app.UseMiddleware<RequestCorrelationMiddleware>();
        app.UseMiddleware<HttpRequestLoggingMiddleware>();
        return app;
    }
}

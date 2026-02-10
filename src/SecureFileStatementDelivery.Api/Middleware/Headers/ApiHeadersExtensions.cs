namespace SecureFileStatementDelivery.Api.Middleware.Headers;

internal static class ApiHeadersExtensions
{
    public static WebApplication UseApiHeaders(this WebApplication app)
    {
        app.UseMiddleware<SecurityHeadersMiddleware>();
        return app;
    }
}

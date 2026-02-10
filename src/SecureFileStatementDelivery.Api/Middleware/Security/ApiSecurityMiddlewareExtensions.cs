namespace SecureFileStatementDelivery.Api.Middleware.Security;

internal static class ApiSecurityMiddlewareExtensions
{
    public static WebApplication UseApiSecurity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}

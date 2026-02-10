namespace SecureFileStatementDelivery.Api.Middleware.Errors;

internal static class ApiErrorHandlingExtensions
{
    public static WebApplication UseApiStatusCodeProblems(this WebApplication app)
    {
        app.UseMiddleware<StatusCodeErrorDetailsMiddleware>();
        return app;
    }
}

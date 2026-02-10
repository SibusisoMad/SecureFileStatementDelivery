using SecureFileStatementDelivery.Application.Interfaces;

namespace SecureFileStatementDelivery.Api.Routes;

internal static class StatusRoutes
{
    public static IEndpointRouteBuilder MapStatusRoutes(this IEndpointRouteBuilder app)
    {
        static async Task<IResult> Handle(IStatusProbe probe, CancellationToken ct)
            => Results.Ok(await probe.GetAsync(ct));

        app.MapGet("/status", Handle)
            .AllowAnonymous()
            .WithName("Status")
            .WithOpenApi();

        return app;
    }
}

using Microsoft.EntityFrameworkCore;
using SecureFileStatementDelivery.Infrastructure.Database;

namespace SecureFileStatementDelivery.Api.Database;

internal static class DatabaseInitializer
{
    public static async Task EnsureCreatedAsync(IServiceProvider services, CancellationToken ct)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SecureFileStatementDeliveryDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}

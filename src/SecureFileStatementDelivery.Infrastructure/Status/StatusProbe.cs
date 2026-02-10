using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecureFileStatementDelivery.Application;
using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Infrastructure.Database;

namespace SecureFileStatementDelivery.Infrastructure.Status;

internal sealed class StatusProbe(SecureFileStatementDeliveryDbContext db, IConfiguration configuration) : IStatusProbe
{
    private readonly string _dataDir = configuration["DATA_DIR"] ?? "/data";

    public async Task<ServiceStatus> GetAsync(CancellationToken ct)
    {
        var canConnectDb = await db.Database.CanConnectAsync(ct);
        var hasStatements = false;
        if (canConnectDb)
        {
            try
            {
                hasStatements = await db.Statements.AsNoTracking().AnyAsync(ct);
            }
            catch
            {
                hasStatements = false;
            }
        }

        var statementsDir = Path.Combine(_dataDir, "statements");
        var statementsDirExists = Directory.Exists(statementsDir);

        return new ServiceStatus(
            Status: "ok",
            CanConnectDb: canConnectDb,
            HasStatements: hasStatements,
            DataDir: _dataDir,
            StatementsDirExists: statementsDirExists);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureFileStatementDelivery.Application.Interfaces;
using SecureFileStatementDelivery.Infrastructure.Database;
using SecureFileStatementDelivery.Infrastructure.Status;
using SecureFileStatementDelivery.Infrastructure.Storage;
using SecureFileStatementDelivery.Infrastructure.Time;
using SecureFileStatementDelivery.Infrastructure.Tokens;

namespace SecureFileStatementDelivery.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var dataDir = configuration["DATA_DIR"] ?? "/data";
        var dbPath = Path.Combine(dataDir, "SecureFileStatementDeliveryDb.db");
        var statementsDir = Path.Combine(dataDir, "statements");

        Directory.CreateDirectory(dataDir);
        Directory.CreateDirectory(statementsDir);

        services.AddDbContext<SecureFileStatementDeliveryDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        services.AddScoped<IStatementRepository, StatementRepository>();
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.AddSingleton<IStatementFileStore>(_ => new FileSystemStatementFileStore(statementsDir));

        services.AddOptions<DownloadTokenOptions>()
            .Bind(configuration.GetSection(DownloadTokenOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.Secret), "DownloadTokens:Secret is required")
            .ValidateOnStart();

        services.AddSingleton<IDownloadTokenService, HmacDownloadTokenService>();
        services.AddScoped<IStatusProbe, StatusProbe>();

        return services;
    }
}

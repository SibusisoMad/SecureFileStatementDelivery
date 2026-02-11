using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecureFileStatementDelivery.Application.Interfaces;

namespace SecureFileStatementDelivery.Api.IntegrationTests;

internal sealed class StatementDeliveryApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "SecureFileStatementDelivery";
    public const string Audience = "SecureFileStatementDelivery";

    public string DataDirectory { get; } = Path.Combine(Path.GetTempPath(), "sfsd-tests", Guid.NewGuid().ToString("n"));

    public ManualTimeProvider Clock { get; } = new ManualTimeProvider(DateTimeOffset.UtcNow);

    public string JwtSigningKey { get; } = "test-signing-key-32chars-minimum!!"; // 32+ chars

    public string DownloadTokenSecret { get; } = "test-download-secret-32chars-minimum"; // 32+ chars

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(configBuilder =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["DATA_DIR"] = DataDirectory,
                ["Database:EnsureCreatedOnStartup"] = "true",
                ["Swagger:Enabled"] = "false",

                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:SigningKey"] = JwtSigningKey,

                ["DownloadTokens:Secret"] = DownloadTokenSecret,
            };

            configBuilder.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            Directory.CreateDirectory(DataDirectory);

            var timeDescriptors = services.Where(d => d.ServiceType == typeof(ITimeProvider)).ToList();
            foreach (var descriptor in timeDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<ITimeProvider>(_ => Clock);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            if (Directory.Exists(DataDirectory))
            {
                Directory.Delete(DataDirectory, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }

        await base.DisposeAsync();
    }
}

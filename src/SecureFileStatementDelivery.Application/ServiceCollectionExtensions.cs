using Microsoft.Extensions.DependencyInjection;
using SecureFileStatementDelivery.Application.Downloads;
using SecureFileStatementDelivery.Application.Statements;

namespace SecureFileStatementDelivery.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<UploadStatementService>();
        services.AddScoped<ListStatementsService>();
        services.AddScoped<CreateDownloadLinkService>();
        services.AddScoped<DownloadStatementService>();

        return services;
    }
}

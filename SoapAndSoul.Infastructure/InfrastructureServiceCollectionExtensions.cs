using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoupApp.ExternalServices;

namespace SoapAndSoul.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection ConfigureInfraSoapAndSoul(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureBlobStorageSettings>(configuration.GetSection("AzureBlobStorage"));
        return services;
    }

    public static IServiceCollection AddInfraSoapAndSoulServices(this IServiceCollection services)
    {
        services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();
        services.AddSingleton<InstrumentationOpenTelemetry>();
        
        return services;
    }
}
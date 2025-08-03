using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoupApp.ExternalServices;

namespace SoapAndSoul.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection ConfigureInfraSoapAndSoul(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public static IServiceCollection AddInfraSoapAndSoulServices(this IServiceCollection services)
    {
        services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();
        
        return services;
    }
}
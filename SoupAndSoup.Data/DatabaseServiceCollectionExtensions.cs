using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoupAndSoup.Data.Services;

namespace SoupAndSoup.Data;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddSoupAndSoulDbServices (this IServiceCollection services)
    {
        services.AddDbContextFactory<SoapAndSoulContext>((serviceProvider, options) =>
        {
            var dbSettings = serviceProvider.GetRequiredService<IOptions<DatabaseSettings>>();
            
            if (dbSettings.Value.IsAzureDb)
                options.UseAzureSql(dbSettings.Value.ConnectionString);
            else
                options.UseSqlServer(dbSettings.Value.ConnectionString);

            if (dbSettings.Value.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        services.AddSingleton<IRecipeService, RecipeService>();
        services.AddSingleton<IComponentService, ComponentService>();
        services.AddSingleton<IComponentTypeService, ComponentTypeService>();
        services.AddSingleton<IMeasureTypesService, MeasureTypesService>();

        return services;
    }
}
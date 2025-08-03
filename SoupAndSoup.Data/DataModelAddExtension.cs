using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SoupAndSoup.Data.Services;

namespace SoupAndSoup.Data;

public static class DataModelAddExtension
{
    public static IServiceCollection AddSoupAndSoulDb (this IServiceCollection services)
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

        services.AddScoped<RecipeService>();
        services.AddScoped<ComponentService>();
        services.AddScoped<ComponentTypeService>();
        services.AddScoped<MeasureTypesService>();

        return services;
    }
}
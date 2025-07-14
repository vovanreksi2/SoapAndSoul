using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoup.Data.Services;

namespace SoupAndSoup.Data;

public static class DataModelAddExtension
{
    public static IServiceCollection AddSoupAndSoulDb (this IServiceCollection services)
    {
        services.AddDbContext<SoapAndSoulContext>(options =>
        {
            options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SoapAndSoulDb_new2;Trusted_Connection=True;")
                .EnableSensitiveDataLogging();

            //options.UseAzureSql(
            //    "Server=tcp:vk-soap-and-soul-sqlserver.database.windows.net,1433;Initial Catalog=soapAndSoulDB;Persist Security Info=False;User ID=admin-vk;Password=Z3f$MX]x]8DoD4yRotV-;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
        });
        services.AddScoped<RecipeService>();
        services.AddScoped<ComponentService>();
        services.AddScoped<ComponentTypeService>();
        services.AddScoped<MeasureTypesService>();

        return services;
    }
}
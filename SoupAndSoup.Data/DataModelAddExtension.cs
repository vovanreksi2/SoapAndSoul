using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoup.Data.Services;

namespace SoupAndSoup.Data
{
    public static class DataModelAddExtension
    {
        public static IServiceCollection AddSoupAndSoulDb (this IServiceCollection services)
        {
            services.AddDbContext<SoapAndSoulContext>(options =>
            {
                options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=SoapAndSoulDb;Trusted_Connection=True;")
                    .EnableSensitiveDataLogging();
            });

            services.AddScoped<RecipeService>();
            services.AddScoped<IngredientService>();
            services.AddScoped<IngredientTypeService>();

            return services;
        }
    }
}

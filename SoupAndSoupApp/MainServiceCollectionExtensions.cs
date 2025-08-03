using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoup.Data;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Services;
using SoupAndSoupApp.ViewModels;
using SoupAndSoupApp.Views;

namespace SoupAndSoupApp;

public static class MainServiceCollectionExtensions
{
    public static IServiceCollection ConfigureSoapAndSoulApp(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseSettings>(configuration.GetSection("DatabaseSettings"));

        return services;
    }

    public static IServiceCollection AddSoapAndSoulAppServices(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<SoapDesignerView>();
        services.AddTransient<SoapDesignerViewModel>();

        services.AddSingleton<AddIngredientDialog>();
        services.AddSingleton<AddIngredientDialogViewModel>();

        services.AddSingleton<IDialogService, DialogService>();

        services.AddSingleton<IDesignerViewModelFactory, DesignerViewModelFactory>();
        services.AddSingleton<IActiveViewModelRegistry, ActiveViewModelRegistry>();

        services.AddSingleton<INotificationService, NotificationService>();

        services.AddSingleton<IAzureBlobStorageService, AzureBlobStorageService>();

        services.AddSingleton<IUnitCostCalculator, UnitCostCalculator>();
        services.AddSingleton<MeasureTypeCache>();
 
        services.AddSoupAndSoulDbServices();
        
        return services;
    }
}
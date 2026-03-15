using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SoapAndSoul.Infrastructure;
using SoupAndSoup.Data;
using SoupAndSoupApp.Helpers.Autosave;
using SoupAndSoupApp.Helpers.Cache;
using SoupAndSoupApp.Helpers.Calculators;
using SoupAndSoupApp.Helpers.Images;
using SoupAndSoupApp.Helpers.Mappers;
using SoupAndSoupApp.Helpers.Navigation;
using SoupAndSoupApp.Helpers.Notifications;
using SoupAndSoupApp.Helpers.Coordinators;
using SoupAndSoupApp.Helpers.Rules;
using SoupAndSoupApp.ViewModels;
using SoupAndSoupApp.Views;

namespace SoupAndSoupApp;

public static class MainServiceCollectionExtensions
{
    public static IServiceCollection ConfigureSoapAndSoulApp(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureInfraSoapAndSoul(configuration);

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
        services.AddSingleton<IFilePickerService, FilePickerService>();

        services.AddSingleton<IDesignerViewModelFactory, DesignerViewModelFactory>();
        services.AddSingleton<IActiveViewModelRegistry, ActiveViewModelRegistry>();

        services.AddSingleton<INotificationService, NotificationService>();

        services.AddSingleton<IUnitCostCalculator, UnitCostCalculator>();
        services.AddSingleton<MeasureTypeCache>();

        // Image service
        services.AddSingleton<IImageService, ImageService>();

        // Mappers
        services.AddSingleton<IRecipeMapper, RecipeMapper>();
        services.AddSingleton<IComponentMapper, ComponentMapper>();

        // Component selection rules
        services.AddSingleton<IComponentSelectionRule, FormSelectionRule>();
        services.AddSingleton<IComponentSelectionRule, EssentialOilSelectionRule>();
        services.AddSingleton<IComponentSelectionRule, FragranceOilSelectionRule>();
        services.AddSingleton<IComponentSelectionRule, CraftingBaseSelectionRule>();
        services.AddSingleton<IComponentSelectionRule, DefaultAmountSelectionRule>();
        services.AddSingleton<IComponentSelectionRuleEngine, ComponentSelectionRuleEngine>();

        // Coordinators
        services.AddTransient<RecipeCoordinator>();
        services.AddTransient<ComponentCoordinator>();
        services.AddTransient<DesignerDataLoader>();

        services.AddInfraSoapAndSoulServices();

        services.AddSoupAndSoulDbServices();
        
        return services;
    }
}
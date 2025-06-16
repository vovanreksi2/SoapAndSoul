using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SoupAndSoup.Data;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.ViewModels;
using SoupAndSoupApp.Views;

namespace SoupAndSoupApp;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        RegisterServices(services);

        ServiceProvider = services.BuildServiceProvider();

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
            {
                desktop.MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                desktop.MainWindow.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
                break;
            }
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = ServiceProvider.GetRequiredService<MainWindow>();
                singleViewPlatform.MainView.DataContext = ServiceProvider.GetRequiredService<MainViewModel>();
                break;
        }


        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<SoapDesignerView>();
        services.AddSingleton<SoapDesignerViewModel>();

        services.AddSingleton<IDialogService, DialogService>();

        services.AddSingleton<AddIngredientDialog>();
        services.AddSingleton<AddIngredientDialogViewModel>();

        services.AddSingleton<IUnitCostCalculator, UnitCostCalculator>();

        services.AddSoupAndSoulDb();
    }
}
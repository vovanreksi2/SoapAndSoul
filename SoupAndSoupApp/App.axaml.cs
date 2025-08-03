using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoupAndSoup.Data;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Services;
using SoupAndSoupApp.ViewModels;
using SoupAndSoupApp.Views;

namespace SoupAndSoupApp;

public class App : Application
{
    private const int DelayAfterSaveMilliseconds = 2000;

    private MainWindow _mainWindow;
    private MainViewModel _mainViewModel;
    
    private IServiceProvider _serviceProvider;
    private ILogger<App> _logger;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void InjectServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
       
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                {
                    desktop.MainWindow = _mainWindow;
                    desktop.MainWindow.DataContext = _mainViewModel;

                    desktop.MainWindow.Closing += MainWindowOnClosing;

                    break;
                }
            case ISingleViewApplicationLifetime singleViewPlatform:

                singleViewPlatform.MainView = _mainWindow;
                singleViewPlatform.MainView.DataContext = _mainViewModel;
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    
    private async void MainWindowOnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = true;

        if (sender is not MainWindow mainWindow) return;

        try
        {
            var saveSuccessful = await SaveAllWithAutoSaveCandidates();
            switch (saveSuccessful)
            {
                case null: //Nothing to save, no candidates needed saving
                    ForceCloseWindow(mainWindow);
                    return;
                case true: //At least one candidate saved successfully
                    await Task.Delay(DelayAfterSaveMilliseconds);
                    ForceCloseWindow(mainWindow);
                    return;
                case false:
                    e.Cancel = true;
                    break;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            
            e.Cancel = true;
        }
    }

    private async Task<bool?> SaveAllWithAutoSaveCandidates()
    {
        var viewModelRegistry = _serviceProvider.GetService<IActiveViewModelRegistry>();
        if (viewModelRegistry == null)
        {
            Console.WriteLine("No active view model registry found.");
            return null; // No candidates to save
        }

        var autoSaveCandidates = viewModelRegistry.GetActiveViewModels();
        if (autoSaveCandidates.All(saveCandidate => !saveCandidate.ShouldSave()))
        {
            Console.WriteLine("At least one auto-save candidate returned null, indicating no save was needed.");
            return null; // No candidates to save
        }

        var tasks = autoSaveCandidates.Select(_ => _.SaveIfNeededAsync());

        var resultTasks = await Task.WhenAll(tasks);
        if (resultTasks.All(r => r)) return true; // All saves were successful

        // Handle the case where at least one save operation failed
        Console.WriteLine("Some auto-save candidates could not be saved.");

        return false;
    }

    private void ForceCloseWindow(object? sender)
    {
        var mainWindow = sender as MainWindow;
        mainWindow.Closing -= MainWindowOnClosing; // Unsubscribe from the event to prevent recursion
        mainWindow.Close();
    }
}

public static class AppBuilderExtensions
{
    public static void UseSoapAndSoulApp(this IServiceCollection services)
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

        services.AddSoupAndSoulDb();
    }
}

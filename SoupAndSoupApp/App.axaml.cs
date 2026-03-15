using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoapAndSoul.Infrastructure;
using SoupAndSoupApp.Helpers;
using SoupAndSoupApp.Helpers.Autosave;
using SoupAndSoupApp.ViewModels;
using SoupAndSoupApp.Views;

namespace SoupAndSoupApp;

public class App : Application
{
    private const int DelayAfterSaveMilliseconds = 2000;
    private static readonly TimeSpan AutoSaveTimeout = TimeSpan.FromSeconds(10);

    private bool _isClosing;
    private MainWindow _mainWindow;
    private MainViewModel _mainViewModel;

    private IServiceProvider _serviceProvider;
    private ILogger<App> _logger;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static IServiceProvider? Services => (Current as App)?._serviceProvider;

    public void InjectServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        _mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
       
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();

        _logger.LogInformation("Application started successfully - ServiceProvider injected");

        var instrumentation = _serviceProvider.GetRequiredService<InstrumentationOpenTelemetry>();
        using var activity = instrumentation.ActivitySource.StartActivity("AppStartup");
        activity?.SetTag("operation", "service_provider_injection");
        activity?.SetTag("app.name", "SoupAndSoupApp");
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
        if (_isClosing) return;

        e.Cancel = true;

        if (sender is not MainWindow mainWindow) return;

        _isClosing = true;
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
                    _isClosing = false;
                    e.Cancel = true;
                    break;
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error during auto-save on window close");
            _isClosing = false;
            e.Cancel = true;
        }
    }

    private async Task<bool?> SaveAllWithAutoSaveCandidates()
    {
        var viewModelRegistry = _serviceProvider.GetService<IActiveViewModelRegistry>();
        if (viewModelRegistry is null)
        {
            _logger.LogWarning("No active view model registry found");
            return null; // No candidates to save
        }

        var autoSaveCandidates = viewModelRegistry.GetActiveViewModels();
        if (autoSaveCandidates.All(saveCandidate => !saveCandidate.ShouldSave()))
        {
            _logger.LogDebug("No auto-save candidates needed saving");
            return null; // No candidates to save
        }

        var tasks = autoSaveCandidates.Select(_ => _.SaveIfNeededAsync());
        var saveAllTask = Task.WhenAll(tasks);

        using var cts = new CancellationTokenSource(AutoSaveTimeout);
        try
        {
            await saveAllTask.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Auto-save timed out after {Timeout}", AutoSaveTimeout);
            return false;
        }

        var resultTasks = await saveAllTask;
        if (resultTasks.All(r => r)) return true; // All saves were successful

        // Handle the case where at least one save operation failed
        _logger.LogWarning("Some auto-save candidates could not be saved");

        return false;
    }

    private void ForceCloseWindow(object? sender)
    {
        if (sender is not MainWindow mainWindow) return;
        mainWindow.Closing -= MainWindowOnClosing; // Unsubscribe from the event to prevent recursion
        mainWindow.Close();
    }
}
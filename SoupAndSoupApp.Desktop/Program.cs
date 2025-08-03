using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SoupAndSoupApp.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                config.AddUserSecrets<Program>(optional: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .AddSoapAndSoulAppServices()
                    .ConfigureSoapAndSoulApp(hostContext.Configuration);
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
            });
        
        BuildAvaloniaApp(hostBuilder.Build())
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(IHost host)
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI()
            .AfterSetup(builder =>
            {
                var app = (App)builder.Instance!;
                app.InjectServiceProvider(host.Services);
            });
    }
}

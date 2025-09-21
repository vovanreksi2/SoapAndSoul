using System;
using Avalonia;
using Avalonia.ReactiveUI;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SoupAndSoupApp.Desktop;

internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {

        var hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, config) => { config.AddUserSecrets<Program>(true); })
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .AddSoapAndSoulAppServices()
                    .ConfigureSoapAndSoulApp(hostContext.Configuration);

                var configuration = hostContext.Configuration;
                var serviceName = "SoupAndSoupApp.Desktop";
                var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";
                var connectionString = configuration["AzureMonitor:ConnectionString"]
                                       ?? configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                                       ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

                services.AddOpenTelemetry()
                    .ConfigureResource(rb => rb.AddService(serviceName, serviceVersion: serviceVersion))
                    .WithTracing(tracing =>
                    {
                        tracing
                            .AddHttpClientInstrumentation()
                            .AddEntityFrameworkCoreInstrumentation(options => { })
                            .AddSqlClientInstrumentation(options =>
                            {
                                options.RecordException = true;
                                options.SetDbStatementForText = false;
                            })
                            .AddSource("SoapAndSoul")
                            .SetSampler(new AlwaysOnSampler());

                        tracing.AddConsoleExporter();

                        if (!string.IsNullOrWhiteSpace(connectionString))
                        {
                            tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = connectionString);
                        }
                    })
                    .WithMetrics(metrics =>
                    {
                        metrics
                            .AddRuntimeInstrumentation()
                            .AddProcessInstrumentation()
                            .AddSqlClientInstrumentation()
                            .AddHttpClientInstrumentation();

                        if (!string.IsNullOrWhiteSpace(connectionString))
                        {
                            metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = connectionString);
                        }
                    });
            })
            .ConfigureLogging((hostContext, logging) =>
            {
                // Load logging configuration from appsettings.json
                logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
               
                var connectionString = hostContext.Configuration["AzureMonitor:ConnectionString"]
                                       ?? hostContext.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
                                       ?? Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");

                logging.AddOpenTelemetry(o =>
                {
                    o.IncludeScopes = true;
                    o.IncludeFormattedMessage = true;
                    
                    if (!string.IsNullOrWhiteSpace(connectionString))
                    {
                        o.AddAzureMonitorLogExporter(config => config.ConnectionString = connectionString);
                    }
                });

            });

        var host = hostBuilder.Build();
        using (host)
        {
            host.Start();  

            BuildAvaloniaApp(host)
                .StartWithClassicDesktopLifetime(args);
        }
    }

    public static AppBuilder BuildAvaloniaApp(IHost host)
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI()
            .AfterSetup(builder =>
            {
                var app = (App)builder.Instance!;
                app.InjectServiceProvider(host.Services);
            });
    }
    
}
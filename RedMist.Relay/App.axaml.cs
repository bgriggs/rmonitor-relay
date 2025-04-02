using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BigMission.Avalonia.LogViewer.Extensions;
using CommunityToolkit.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.DependencyInjection;
using LogViewer.Core.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MsLogger.Core;
using RedMist.Relay.Common;
using RedMist.Relay.Services;
using RedMist.Relay.Services.X2Test;
using RedMist.Relay.ViewModels;
using RedMist.Relay.Views;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;

namespace RedMist.Relay;

public partial class App : Application
{
    private IHost? _host;
    private CancellationTokenSource? _cancellationTokenSource;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        // Dependency injection: https://github.com/stevemonaco/AvaloniaViewModelFirstDemos
        // NuGet source: https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-Labs/nuget/v3/index.json
        var locator = new ViewLocator();
        DataTemplates.Add(locator);

        var builder = Host.CreateApplicationBuilder();
        builder.AddLogViewer().Logging.AddDefaultDataStoreLogger();

        // Manually resolve dependency assembly references from the same directory as the main assembly
        // since the MyLaps SDK is not loading the required DLLs from the same directory, i.e. SDKWrapper.dll
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var assemblyName = new AssemblyName(args.Name).Name + ".dll";
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, assemblyName);

            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }
            return null; // Let the default resolution process continue
        };

        var services = builder.Services;
        ConfigureServices(services);
        ConfigureViewModels(services);
        ConfigureViews(services);
        ConfigureDynamicServices(services);

        services.AddSingleton(service => new MainWindow
        {
            DataContext = service.GetRequiredService<MainViewModel>()
        });

        _host = builder.Build();
        _cancellationTokenSource = new();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var provider = services.BuildServiceProvider();
            Ioc.Default.ConfigureServices(provider);

            desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
            desktop.ShutdownRequested += OnShutdownRequested;

            _host.Services.GetRequiredService<OrbitsLogService>().Initialize();
        }

        // Startup background services
        _ = _host.StartAsync(_cancellationTokenSource.Token);

        base.OnFrameworkInitializationCompleted();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
       => _ = _host!.StopAsync(_cancellationTokenSource!.Token);


    [Singleton(typeof(WindowsSettingsProvider), typeof(ISettingsProvider))]
    [Singleton(typeof(HubClient))]
    [Singleton(typeof(EventDataCache))]
    [Singleton(typeof(RelayService))]
    [Singleton(typeof(RMonitorClient))]
    [Singleton(typeof(OrganizationClient))]
    [Singleton(typeof(EventManagementClient))]
    [Singleton(typeof(EventService))]
    [Singleton(typeof(OrganizationConfigurationService))]
    [Singleton(typeof(OrbitsLogService))]
    internal static partial void ConfigureServices(IServiceCollection services);

    [Singleton(typeof(MainViewModel))]
    [Singleton(typeof(LogViewerControlViewModel))]
    internal static partial void ConfigureViewModels(IServiceCollection services);

    [Singleton(typeof(MainView))]
    [Singleton(typeof(EditOrganizationDialog))]
    [Singleton(typeof(EditOrbitsDialog))]
    [Singleton(typeof(EditX2ServerDialog))]
    [Singleton(typeof(EditControlLogDialog))]
    [Singleton(typeof(EditEventDialog))]
    internal static partial void ConfigureViews(IServiceCollection services);

    /// <summary>
    /// MyLaps code cannot be included so load dlls dynamically.
    /// </summary>
    private void ConfigureDynamicServices(IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(GetType().Name);
        
        if (bool.Parse(configuration["X2Test"] ?? "false"))
        {
            services.AddSingleton<IX2Client, X2TestClient>();
            return;
        }

        var x2Path = configuration["X2DllPath"];
        if (File.Exists(x2Path))
        {
            x2Path = Path.GetFullPath(x2Path);
            logger.LogInformation("Loading X2 DLL from {0}", x2Path);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(x2Path);
            var serviceType = typeof(IX2Client);
            var implementation = assembly.GetTypes().FirstOrDefault(t => serviceType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
            if (implementation is not null)
            {
                services.AddSingleton(serviceType, implementation);
                logger.LogInformation("X2 DLL loaded successfully");
            }
        }
        else
        {
            logger.LogWarning("X2 DLL not found at {0}", x2Path);
            services.AddSingleton<IX2Client, NullX2Client>();
        }
    }

    private void TrayIcon_Clicked(object? sender, System.EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is { IsVisible: false })
            {
                desktop.MainWindow.Show();
            }
        }
    }

    private void OpenMenuItem_Click(object? sender, System.EventArgs e)
    {
        TrayIcon_Clicked(null, new System.EventArgs());
    }

    private void ExitMenuItem_Click(object? sender, System.EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}

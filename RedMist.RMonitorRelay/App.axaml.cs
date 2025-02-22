using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using BigMission.Avalonia.LogViewer.Extensions;
using CommunityToolkit.Extensions.DependencyInjection;
using LogViewer.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MsLogger.Core;
using RedMist.RMonitorRelay.Services;
using RedMist.RMonitorRelay.ViewModels;
using RedMist.RMonitorRelay.Views;
using System.Threading;

namespace RedMist.RMonitorRelay;

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

        var services = builder.Services;
        ConfigureServices(services);
        ConfigureViewModels(services);
        ConfigureViews(services);

        services.AddSingleton(service => new MainWindow
        {
            DataContext = service.GetRequiredService<MainViewModel>()
        });

        _host = builder.Build();
        _cancellationTokenSource = new();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = _host.Services.GetRequiredService<MainWindow>();
            desktop.ShutdownRequested += OnShutdownRequested;
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
    [Singleton(typeof(Relay))]
    internal static partial void ConfigureServices(IServiceCollection services);

    [Singleton(typeof(MainViewModel))]
    [Singleton(typeof(LogViewerControlViewModel))]
    [Singleton(typeof(RMonitorClient))]
    internal static partial void ConfigureViewModels(IServiceCollection services);

    [Singleton(typeof(MainView))]
    internal static partial void ConfigureViews(IServiceCollection services);

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

using CommunityToolkit.Mvvm.ComponentModel;
using LogViewer.Core.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace RedMist.Relay.ViewModels;

public partial class MainViewModel : ObservableValidator
{
    private readonly Services.RelayService relay;

    public LogViewerControlViewModel LogViewer { get; }

    public OrganizationViewModel Organization { get; }
    public OrbitsViewModel Orbits { get; }
    public X2ServerViewModel X2Server { get; }
    public ControlLogViewModel ControlLog { get; }

    [ObservableProperty]
    private bool? enableLogMessages = true;


    public MainViewModel(ISettingsProvider settings, Services.RelayService relay, LogViewerControlViewModel logViewer, IConfiguration configuration,
        OrganizationClient organizationClient, EventManagementClient eventManagementClient, ILoggerFactory loggerFactory, OrganizationConfigurationService configurationService)
    {
        this.relay = relay;
        LogViewer = logViewer;

        Organization = new OrganizationViewModel(configurationService, settings, loggerFactory);
        Orbits = new OrbitsViewModel(loggerFactory, configurationService);
        X2Server = new X2ServerViewModel(configurationService, loggerFactory, configuration);
        ControlLog = new ControlLogViewModel(loggerFactory, configurationService, organizationClient);
        relay.SetLocalMessageLogging(EnableLogMessages ?? false);
    }


    public void Initialize()
    {
        _ = relay.StartHubAsync();
        Organization.Initialize();
        Orbits.Initialize();
        ControlLog.Initialize();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(EnableLogMessages))
        {
            relay.SetLocalMessageLogging(EnableLogMessages ?? false);
        }
    }
    public void OpenLogs()
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        path = Path.Combine(path, "RedMist");
        if (Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = path,
                UseShellExecute = true
            });
        }
    }
}

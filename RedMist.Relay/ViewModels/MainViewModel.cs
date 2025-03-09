using CommunityToolkit.Mvvm.ComponentModel;
using LogViewer.Core.ViewModels;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Services;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class MainViewModel : ObservableValidator
{
    private readonly ISettingsProvider settings;
    private readonly Services.Relay relay;
    private readonly OrganizationClient organizationClient;
    private readonly EventManagementClient eventManagementClient;
    private readonly ILoggerFactory loggerFactory;

    public LogViewerControlViewModel LogViewer { get; }

    public OrganizationViewModel Organization { get; }
    public OrbitsViewModel Orbits { get; }


    [ObservableProperty]
    private bool? enableLogMessages = true;


    public MainViewModel(ISettingsProvider settings, Services.Relay relay, LogViewerControlViewModel logViewer,
        OrganizationClient organizationClient, EventManagementClient eventManagementClient, ILoggerFactory loggerFactory)
    {
        this.settings = settings;
        this.relay = relay;
        LogViewer = logViewer;
        this.organizationClient = organizationClient;
        this.eventManagementClient = eventManagementClient;
        this.loggerFactory = loggerFactory;

        Organization = new OrganizationViewModel(organizationClient, settings, loggerFactory);
        Orbits = new OrbitsViewModel(organizationClient, loggerFactory);

        relay.SetLocalMessageLogging(EnableLogMessages ?? false);
    }


    public async Task Initialize()
    {
        _ = relay.StartHubAsync();
        var org = await Organization.Initialize();
        if (org != null && org.Orbits != null)
        {
            _ = relay.StartOrbitsAsync(org.Orbits.IP, org.Orbits.Port);
        }

        _ = Orbits.Initialize(org);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(EnableLogMessages))
        {
            relay.SetLocalMessageLogging(EnableLogMessages ?? false);
        }
    }

    //public async Task ConnectAsync()
    //{
    //    IsConnectionBusy = true;
    //    try
    //    {
    //        var result = await relay.StartAsync(Ip, Port);
    //        if (result)
    //        {
    //            IsConnected = true;
    //        }
    //    }
    //    finally
    //    {
    //        IsConnectionBusy = false;
    //    }
    //}

    //public async Task DisconnectAsync()
    //{
    //    IsConnectionBusy = true;
    //    try
    //    {
    //        await relay.StopAsync();
    //    }
    //    finally
    //    {
    //        IsConnected = false;
    //        IsConnectionBusy = false;
    //    }
    //}



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

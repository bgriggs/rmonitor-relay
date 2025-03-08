using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LogViewer.Core.ViewModels;
using RedMist.Relay.Services;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class MainViewModel : ObservableValidator
{
    private readonly ISettingsProvider settings;
    private readonly Services.Relay relay;
    public LogViewerControlViewModel LogViewer { get; }

    private string ip = string.Empty;
    [CustomValidation(typeof(MainViewModel), nameof(IpValidate))]
    public string Ip
    {
        get => ip;
        set => SetProperty(ref ip, value, validate: true);
    }

    private int port = 50000;
    [Range(1, 65535)]
    public int Port
    {
        get => port;
        set => SetProperty(ref port, value, validate: true);
    }

    private string clientSecret = string.Empty;
    [StringLength(32, MinimumLength = 5)]
    public string ClientSecret
    {
        get => clientSecret;
        set => SetProperty(ref clientSecret, value);
    }


    [ObservableProperty]
    private bool isConnected = false;
    [ObservableProperty]
    private bool isConnectionBusy = false;
    [ObservableProperty]
    private string hubConnectionState = "Disconnected";
    [ObservableProperty]
    private int messagesReceived;
    [ObservableProperty]
    private int messagesSent;
    [ObservableProperty]
    private bool? enableLogMessages = true;


    public MainViewModel(ISettingsProvider settings, Services.Relay relay, LogViewerControlViewModel logViewer)
    {
        this.settings = settings;
        this.relay = relay;
        LogViewer = logViewer;

        relay.ConnectionStatusChanged += async (state) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => HubConnectionState = state.ToString());
        };

        relay.MessageCountChanged += async (count) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MessagesReceived = count.rx;
                MessagesSent = count.tx;
            });
        };

        relay.SetLocalMessageLogging(EnableLogMessages ?? false);
    }


    public void LoadSettings()
    {
        Ip = settings.GetWithOverride("RMonitorIP") ?? "127.0.0.1";
        Port = int.TryParse(settings.GetWithOverride("RMonitorPort"), out var port) ? port : 50000;
        ClientSecret = settings.GetWithOverride("Keycloak:ClientSecret") ?? string.Empty;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(Ip))
        {
            settings.SaveUser("RMonitorIP", Ip);
        }
        else if (e.PropertyName == nameof(Port))
        {
            settings.SaveUser("RMonitorPort", Port.ToString());
        }
        else if (e.PropertyName == nameof(ClientSecret))
        {
            settings.SaveUser("Keycloak:ClientSecret", ClientSecret);
        }
        else if (e.PropertyName == nameof(EnableLogMessages))
        {
            relay.SetLocalMessageLogging(EnableLogMessages ?? false);
        }
    }

    public async Task ConnectAsync()
    {
        IsConnectionBusy = true;
        try
        {
            var result = await relay.StartAsync(Ip, Port);
            if (result)
            {
                IsConnected = true;
            }
        }
        finally
        {
            IsConnectionBusy = false;
        }
    }

    public async Task DisconnectAsync()
    {
        IsConnectionBusy = true;
        try
        {
            await relay.StopAsync();
        }
        finally
        {
            IsConnected = false;
            IsConnectionBusy = false;
        }
    }

    public static ValidationResult IpValidate(string host, ValidationContext context)
    {
        if (IPAddress.TryParse(host, out _))
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult("IP Address is not valid");
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

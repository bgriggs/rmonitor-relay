using BigMission.Shared.Utilities;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Common;
using RedMist.Relay.Models;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

/// <summary>
/// Takes data received from RMonitor and X2 endpoints and sends to the cloud SignalR hub.
/// </summary>
public class RelayService : IRecipient<OrganizationConfigurationChanged>, IRecipient<EventConfigurationChanged>,
    IRecipient<X2ReceivedLoop>, IRecipient<X2ReceivedPassing>
{
    private readonly HubClient hubClient;
    private readonly RMonitorClient rMonitorClient;
    private readonly EventDataCache eventDataCache;
    private readonly EventService eventService;
    private readonly IConfiguration configuration;
    private readonly IX2Client? x2Client;

    public event Action<HubConnectionState>? ConnectionStatusChanged;
    private int rmonitorMessagesReceived;
    private bool lastRmonitorConnected;
    private HubConnectionState lastHubState = HubConnectionState.Disconnected;
    public HubConnectionState HubConnectionState => lastHubState;
    private FileStream? localLoggingStream;

    private ILogger Logger { get; }
    private IDisposable? orbitsConnectionCheckSubscription;
    private bool isX2Stated;
    private X2Configuration? x2Configuration;


    public RelayService(ILoggerFactory loggerFactory, HubClient hubClient, RMonitorClient rMonitorClient,
        EventDataCache eventDataCache, EventService eventService, IX2Client x2Client, IConfiguration configuration)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
        this.hubClient = hubClient;
        this.rMonitorClient = rMonitorClient;
        this.eventDataCache = eventDataCache;
        this.eventService = eventService;
        this.configuration = configuration;
        if (x2Client.GetType() != typeof(NullX2Client))
        {
            this.x2Client = x2Client;
        }

        rMonitorClient.ReceivedData += async (data) => await RMonitorReceiveData(data);

        hubClient.ReceivedSendEventData += async () => await SendCachedMessagesAsync();
        hubClient.ConnectionStatusChanged += async (state) => await OnHubConnectionChanged(state);

        eventDataCache.SessionChanged += async (e) => await hubClient.SendSessionChangeAsync(eventService.Event?.Id ?? 0, e.sessionId, e.name);

        WeakReferenceMessenger.Default.RegisterAll(this);
    }


    #region Hub

    public async Task StartHubAsync()
    {
        await hubClient.StartAsync(CancellationToken.None);
    }

    private async Task OnHubConnectionChanged(HubConnectionState state)
    {
        ConnectionStatusChanged?.Invoke(state);

        if (state != lastHubState)
        {
            lastHubState = state;
            Logger.LogDebug("Hub connection state changed: {0}", state);
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<HubConnectionState>(state));
        }
        else
        {
            return;
        }

        // On server connected, resend cached data
        if (state == HubConnectionState.Connected)
        {
            await SendCachedMessagesAsync();
            await SendCachedPassingsAsync();
        }
    }

    private async Task SendCachedMessagesAsync()
    {
        var cached = await eventDataCache.GetRMonitorData();
        try
        {
            if (eventService.Event != null)
            {
                if (!string.IsNullOrEmpty(eventDataCache.SessionName))
                {
                    Logger.LogDebug("Sending session data to hub: {0}, {1}", eventDataCache.SessionNumber, eventDataCache.SessionName);
                    await hubClient.SendSessionChangeAsync(eventService.Event.Id, eventDataCache.SessionNumber, eventDataCache.SessionName);
                }

                Logger.LogDebug($"Sending cached messages to hub");
                await hubClient.SendRMonitorAsync(eventService.Event.Id, eventDataCache.SessionNumber, cached);
            }
            else
            {
                Logger.LogWarning("No event selected to send cached data to hub");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send cached data to hub");
        }
    }

    #endregion

    #region Orbits Result Monitor

    public async Task<bool> StartOrbitsAsync(string rmonitorIp, int rmonitorPort, CancellationToken cancellationToken = default)
    {
        orbitsConnectionCheckSubscription ??= Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => CheckFireOrbitsConnectionChanged());
        return await rMonitorClient.ConnectAsync(rmonitorIp, rmonitorPort, cancellationToken);
    }

    /// <summary>
    /// Aggregate result monitor data locally and to the cloud.
    /// </summary>
    /// <param name="data">rmonitor raw data</param>
    private async Task RMonitorReceiveData(string data)
    {
        try
        {
            rmonitorMessagesReceived++;
            await CheckForInit(data);

            // Process the cache to ensure we catch changes in sessions ($B) before sending to the hub with the incorrect session number
            await eventDataCache.UpdateRMonitor(data);

            if (eventService.Event != null)
            {
                await hubClient.SendRMonitorAsync(eventService.Event.Id, eventDataCache.SessionNumber, data);
            }
            WeakReferenceMessenger.Default.Send(new RMonitorMessageStatistic(rmonitorMessagesReceived));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send data to hub");
        }
    }

    /// <summary>
    /// Clear cache on init command.
    /// </summary>
    private async Task CheckForInit(string data)
    {
        var parts = data.Split('\n');
        foreach (var p in parts)
        {
            if (p.StartsWith("$I"))
            {
                Logger.LogInformation("RMonitor init data received. Clearing Cache.");
                await eventDataCache.Clear();
            }
        }
    }

    /// <summary>
    /// Manually determines when connection has changed on the Orbits connection.
    /// </summary>
    private void CheckFireOrbitsConnectionChanged()
    {
        var isConnected = rMonitorClient.IsConnected;
        if (isConnected != lastRmonitorConnected)
        {
            lastRmonitorConnected = isConnected;
            Logger.LogDebug("RMonitor connection state changed: {0}", lastRmonitorConnected);

            var cs = lastRmonitorConnected ? ConnectionState.Connected : ConnectionState.Disconnected;
            WeakReferenceMessenger.Default.Send(new OrbitsConnectionState(cs));
        }
    }

    #endregion

    #region X2

    public void StartX2(CancellationToken cancellationToken = default)
    {
        if (x2Client == null || isX2Stated)
            return;

        isX2Stated = true;

        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (x2Client?.ConnectionState == ConnectionState.Disconnected)
                    {
                        var config = x2Configuration;
                        if (config != null)
                        {
                            var key = configuration["AesKey"] ?? throw new ArgumentNullException("AesKey not found");
                            var encryption = new EncryptionService(key, Consts.IV);
                            var password = encryption.Decrypt(config.Password);

                            bool? result = x2Client?.Connect(config.Server, config.Username, password);
                            if (result is not true)
                            {
                                Logger.LogWarning("Failed to connect to X2 server. Retrying in 10 seconds.");
                                await Task.Delay(TimeSpan.FromSeconds(7));
                            }
                        }
                        else
                        {
                            Logger.LogDebug("X2 configuration not found. Not able to start connection to X2 server.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Failed to start X2 connection");
                }

                await Task.Delay(TimeSpan.FromSeconds(3));
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Receives X2 Loop changes.
    /// </summary>
    public void Receive(X2ReceivedLoop message)
    {
        _ = Task.Run(async () =>
        {
            if (eventService.Event != null)
            {
                await hubClient.SendLoopsAsync(eventService.Event.Id, message.Loops);
            }
        });
    }

    /// <summary>
    /// Receives X2 Passing changes.
    /// </summary>
    public void Receive(X2ReceivedPassing message)
    {
        _ = Task.Run(async () =>
        {
            if (eventService.Event != null)
            {
                bool result = await hubClient.SendPassingsAsync(eventService.Event.Id, message.Passings);
                if (!result)
                {
                    eventDataCache.UpdatePassings(message.Passings);
                }
            }
        });
    }

    private async Task SendCachedPassingsAsync()
    {
        var passings = eventDataCache.GetPassingsWithClear();
        if (passings.Count != 0 && eventService.Event != null)
        {
            await hubClient.SendPassingsAsync(eventService.Event.Id, passings);
        }
    }

    #endregion

    #region Notifications

    /// <summary>
    /// Apply change in configuration.
    /// </summary>
    /// <param name="message">new configuration</param>
    public void Receive(OrganizationConfigurationChanged message)
    {
        hubClient.ReloadClientCredentials();

        if (message.Organization?.Orbits != null)
        {
            _ = Task.Run(async () =>
            {
                await rMonitorClient.DisconnectAsync(CancellationToken.None);
                await Task.Delay(3000); // Allow reconnect loops to time out
                await StartOrbitsAsync(message.Organization.Orbits.IP, message.Organization.Orbits.Port, CancellationToken.None);
                Logger.LogInformation("Orbits connection changed: {0}", message.Organization.Orbits.IP);
            });
        }

        x2Configuration = message.Organization?.X2;
        if (x2Configuration != null)
        {
            StartX2(CancellationToken.None);
        }
    }

    /// <summary>
    /// Event changed, update the cloud.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(EventConfigurationChanged message)
    {
        _ = SendCachedMessagesAsync();
    }

    #endregion

    #region Logging

    public void SetLocalMessageLogging(bool enabled)
    {
        if (enabled)
        {
            rMonitorClient.ReceivedData += LogLocalMessage;
        }
        else
        {
            rMonitorClient.ReceivedData -= LogLocalMessage;
        }
    }

    private void LogLocalMessage(string data)
    {
        try
        {
            if (localLoggingStream is null)
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                path = Path.Combine(path, "RedMist");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, $"redmist-relay-messages{DateTime.Now:HH-mm-ss-MM-dd-yyyy}.log");
                Logger.LogInformation("Logging local messages to {0}", path);
                localLoggingStream = File.OpenWrite(path);
            }

            var buf = Encoding.UTF8.GetBytes($"##{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}{data}{Environment.NewLine}");
            localLoggingStream.Write(buf);
            localLoggingStream.Flush();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to log local message");
        }
    }

    #endregion
}

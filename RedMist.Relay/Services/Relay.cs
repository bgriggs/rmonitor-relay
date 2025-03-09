using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Models;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

/// <summary>
/// Take data received from RMonitor endpoint and send to the SignalR hub.
/// </summary>
public class Relay : IRecipient<OrganizationConnectionChanged>
{
    private readonly HubClient hubClient;
    private readonly RMonitorClient rMonitorClient;
    private readonly EventDataCache eventDataCache;
    public event Action<HubConnectionState>? ConnectionStatusChanged;
    private int rmonitorMessagesReceived;
    private bool lastRmonitorConnected;
    private HubConnectionState lastHubState = HubConnectionState.Disconnected;
    private FileStream? localLoggingStream;

    private ILogger Logger { get; }
    private IDisposable? consistencyCheckInterval;


    public Relay(ILoggerFactory loggerFactory, HubClient hubClient, RMonitorClient rMonitorClient, EventDataCache eventDataCache)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
        this.hubClient = hubClient;
        this.rMonitorClient = rMonitorClient;
        this.eventDataCache = eventDataCache;
        rMonitorClient.ReceivedData += async (data) => await RMonitorReceiveData(data);

        hubClient.ReceivedSendEventData += async () => await SendCachedMessagesAsync();
        hubClient.ConnectionStatusChanged += async (state) => await OnHubConnectionChanged(state);

        eventDataCache.EventChanged += async (e) => await hubClient.SendEventUpdate(e.eventId, e.name);
    }

    #region Hub

    public async Task StartHubAsync()
    {
        await hubClient.StartAsync(CancellationToken.None);
    }

    public void Receive(OrganizationConnectionChanged message)
    {
        hubClient.ReloadClientCredentials();
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
        }
    }

    private async Task SendCachedMessagesAsync()
    {
        var cached = await eventDataCache.GetData();
        try
        {
            if (eventDataCache.EventNumber > 0)
            {
                Logger.LogDebug("Sending event data to hub: {0}, {1}", eventDataCache.EventNumber, eventDataCache.EventName);
                await hubClient.SendEventUpdate(eventDataCache.EventNumber, eventDataCache.EventName);
            }

            Logger.LogDebug($"Sending cached messages to hub");
            await hubClient.SendAsync(eventDataCache.EventNumber, cached);
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
        if (consistencyCheckInterval == null)
        {
            consistencyCheckInterval = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ => CheckOrbitsConnection());
        }
        return await rMonitorClient.ConnectAsync(rmonitorIp, rmonitorPort, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await rMonitorClient.DisconnectAsync(cancellationToken);
    }

    private async Task RMonitorReceiveData(string data)
    {
        try
        {
            rmonitorMessagesReceived++;
            await CheckForInit(data);
            await eventDataCache.Update(data);
            await hubClient.SendAsync(eventDataCache.EventNumber, data);
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

    private void CheckOrbitsConnection()
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

using BigMission.Shared.SignalR;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

/// <summary>
/// Client for the Red Mist timing signalR hub.
/// </summary>
public class HubClient : HubClientBase
{
    private HubConnection? hub;
    private ILogger Logger { get; }
    public event Action? ReceivedSendEventData;
    private CancellationToken stoppingToken;
    public int MessagesSent { get; private set; }

    public HubClient(ILoggerFactory loggerFactory, IConfiguration configuration) : base(loggerFactory, configuration)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.stoppingToken = stoppingToken;
        hub = StartConnection(stoppingToken);
        hub.On("SendEventData", () => ReceivedSendEventData?.Invoke());

        while (!stoppingToken.IsCancellationRequested)
        {
            FireStatusUpdate(hub);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    public async Task<bool> SendRMonitor(int eventId, string data)
    {
        if (hub is null || hub.State != HubConnectionState.Connected)
        {
            Logger.LogTrace("Hub not connected, unable to send: {0}", data);
            return false;
        }

        await hub.SendAsync("SendRMonitor", eventId, data, stoppingToken);
        MessagesSent++;
        WeakReferenceMessenger.Default.Send(new HubMessageStatistic(MessagesSent));
        return true;
    }

    public async Task SendEventUpdate(int eventId, string eventName)
    {
        try
        {
            if (hub is null || hub.State != HubConnectionState.Connected)
            {
                Logger.LogTrace("Hub not connected, unable to send event update");
                return;
            }
            await hub.SendAsync("SendEventUpdate", eventId, eventName, stoppingToken);
            MessagesSent++;
            WeakReferenceMessenger.Default.Send(new HubMessageStatistic(MessagesSent));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to send event update");
        }
    }
}

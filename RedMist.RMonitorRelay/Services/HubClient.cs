using BigMission.Shared.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.RMonitorRelay.Services;

/// <summary>
/// Client for the Red Mist timing signalR hub.
/// </summary>
public class HubClient : HubClientBase
{
    private HubConnection? hub;
    private ILogger Logger { get; }
    public event Action? ReceivedSendEventData;

    public HubClient(ILoggerFactory loggerFactory, IConfiguration configuration) : base(loggerFactory, configuration)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        hub = StartConnection(stoppingToken);
        hub.On("SendEventData", () => ReceivedSendEventData?.Invoke());

        while (!stoppingToken.IsCancellationRequested)
        {
            FireStatusUpdate(hub);
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }

    public async Task<bool> SendAsync(string data)
    {
        if (hub is null)
        {
            Logger.LogWarning("Hub not connected, unable to send: {0}", data);
            return false;
        }

        await hub.SendAsync("Send", data);
        return true;
    }
}

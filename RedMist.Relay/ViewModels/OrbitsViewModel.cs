using Avalonia.Threading;
using BigMission.Shared.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using RedMist.Relay.Models;
using System;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class OrbitsViewModel : ObservableValidator, IRecipient<RMonitorMessageStatistic>, IRecipient<OrbitsConnectionState>
{
    [ObservableProperty]
    private int rmonitorMessagesReceived;

    [ObservableProperty]
    private string rmonitorConnectionStr = "Disconnected";

    [ObservableProperty]
    private ConnectionState rmonitorConnectionState = ConnectionState.Disconnected;

    private readonly Debouncer debouncer = new(TimeSpan.FromMilliseconds(500));


    public OrbitsViewModel()
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
    }


    public void EditOrbits()
    {

    }

    public void Receive(RMonitorMessageStatistic message)
    {
        _ = debouncer.ExecuteAsync(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                RmonitorMessagesReceived = message.Value;
            }, DispatcherPriority.Background);
            return Task.CompletedTask;
        });
    }

    public void Receive(OrbitsConnectionState message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RmonitorConnectionStr = message.Value.ToString();
            RmonitorConnectionState = message.Value;
        }, DispatcherPriority.Background);
    }
}

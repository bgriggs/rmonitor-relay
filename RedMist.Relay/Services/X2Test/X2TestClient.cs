using CommunityToolkit.Mvvm.Messaging;
using RedMist.Relay.Common;
using RedMist.TimingCommon.Models.X2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RedMist.Relay.Services.X2Test;

internal class X2TestClient : IX2Client
{
    const string LoopsPath = "Services/X2Test/Loops.json";
    const string PassingsPath = "Services/X2Test/Passings.json";
    const double REPLAY_SPEED = 10.0;
    public int MessagesSent { get; private set; }

    private readonly List<(DateTime Timestamp, Passing data)> passings = [];

    private ConnectionState connectionState = ConnectionState.Disconnected;
    public ConnectionState ConnectionState {
        get => connectionState;
        private set
        {
            if (value != connectionState)
            {
                connectionState = value;
                WeakReferenceMessenger.Default.Send(new X2ConnectionState(connectionState));
            }
        }
    }

    public bool Connect(string hostname, string username, string password)
    {
        if (File.Exists(LoopsPath) && File.Exists(PassingsPath))
        {
            //ConnectionState = ConnectionState.Connecting;

            ConnectionState = ConnectionState.Connected;

            Task.Run(RunPassings);
            return true;
        }

        ConnectionState = ConnectionState.Disconnected;
        return false;
    }

    public List<Loop>? GetLoops()
    {
        var loopsJson = File.ReadAllText(LoopsPath);
        return System.Text.Json.JsonSerializer.Deserialize<List<Loop>>(loopsJson);
    }

    public void ResendPassings(DateTime start, DateTime? end) { }

    private void RunPassings()
    {
        var json = File.ReadAllText(PassingsPath);
        var passings = System.Text.Json.JsonSerializer.Deserialize<List<Passing>>(json);
        var lastTimestamp = DateTime.MinValue;
        foreach (var passing in passings!)
        {
            MessagesSent++;
            WeakReferenceMessenger.Default.Send(new X2MessageStatistic(MessagesSent));
            WeakReferenceMessenger.Default.Send(new X2ReceivedPassing([passing]));

            if (lastTimestamp != DateTime.MinValue)
            {
                var delay = passing.TimestampLocal - lastTimestamp;
                var duration = (int)(delay.TotalMilliseconds / REPLAY_SPEED);
                if (duration < 0)
                    duration = 0;
                //else if (duration > 10000)
                //    duration = 10000;
                System.Threading.Thread.Sleep(duration);
            }
            lastTimestamp = passing.TimestampLocal;
        }
    }
}

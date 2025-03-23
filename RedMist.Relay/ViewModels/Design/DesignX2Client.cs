using RedMist.Relay.Common;
using RedMist.TimingCommon.Models.X2;
using System;
using System.Collections.Generic;

namespace RedMist.Relay.ViewModels.Design;

public class DesignX2Client : IX2Client
{
    public ConnectionState ConnectionState => ConnectionState.Connected;

    public bool Connect(string hostname, string username, string password)
    {
        return true;
    }

    public List<Loop>? GetLoops()
    {
        return [new Loop { Id = 123123, Name = "Start/Finish" }];
    }

    public void ResendPassings(DateTime start, DateTime? end)
    {
    }
}

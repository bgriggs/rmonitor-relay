using RedMist.Relay.Common;
using RedMist.TimingCommon.Models.X2;
using System;
using System.Collections.Generic;

namespace RedMist.Relay.Services;

internal class NullX2Client : IX2Client
{
    public ConnectionState ConnectionState => ConnectionState.Disconnected;

    public bool Connect(string hostname, string username, string password)
    {
        return false;
    }

    public List<Loop>? GetLoops()
    {
        return null;
    }

    public void ResendPassings(DateTime start, DateTime? end)
    {
    }
}

using CommunityToolkit.Mvvm.Messaging.Messages;
using RedMist.Relay.Common;

namespace RedMist.Relay.Models;

/// <summary>
/// Connection status with the Orbits/RMonitor timing system.
/// </summary>
/// <param name="value"></param>
public class OrbitsConnectionState(ConnectionState value) : ValueChangedMessage<ConnectionState>(value)
{
}

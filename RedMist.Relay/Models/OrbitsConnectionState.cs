using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RedMist.Relay.Models;

/// <summary>
/// Connection status with the Orbits/RMonitor timing system.
/// </summary>
/// <param name="value"></param>
public class OrbitsConnectionState(ConnectionState value) : ValueChangedMessage<ConnectionState>(value)
{
}

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RedMist.Relay.Models;

public class OrbitsConnectionState(ConnectionState value) : ValueChangedMessage<ConnectionState>(value)
{
}

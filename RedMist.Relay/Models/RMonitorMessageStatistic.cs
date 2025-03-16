using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RedMist.Relay.Models;

/// <summary>
/// Messages received from the timing system.
/// </summary>
/// <param name="value"></param>
public class RMonitorMessageStatistic(int value) : ValueChangedMessage<int>(value)
{
}

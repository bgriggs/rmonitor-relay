using CommunityToolkit.Mvvm.Messaging.Messages;

namespace RedMist.Relay.Models;

/// <summary>
/// Messages sent to the cloud.
/// </summary>
/// <param name="value"></param>
public class RMonitorMessageStatistic(int value) : ValueChangedMessage<int>(value)
{
}

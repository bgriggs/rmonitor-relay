using RedMist.TimingCommon.Models.Configuration;

namespace RedMist.Relay.Models;

/// <summary>
/// Notification that the event configuration has changed, such as when user selects a new event.
/// </summary>
/// <param name="event"></param>
public class EventConfigurationChanged(Event? @event)
{
    public Event? Event { get; } = @event;
}

using RedMist.TimingCommon.Models.Configuration;

namespace RedMist.Relay.Models;

public class EventConfigurationChanged(Event? @event)
{
    public Event? Event { get; } = @event;
}

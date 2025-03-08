using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

public class DesignRelay : Services.Relay
{
    public DesignRelay() : base(new DebugLoggerFactory(), new DesignHubClient(), new DesignRMonitorClient(), new EventDataCache()) { }
}

using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

public class DesignRelay : RelayService
{
    public DesignRelay() : base(new DebugLoggerFactory(), new DesignHubClient(), new DesignRMonitorClient(),
        new EventDataCache(), new DesignEventService(), new DesignX2Client(), new DesignConfiguration())
    { }
}

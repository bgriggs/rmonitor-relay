using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

class DesignRMonitorClient : RMonitorClient
{
    public DesignRMonitorClient() : base(new DebugLoggerFactory()) { }
}

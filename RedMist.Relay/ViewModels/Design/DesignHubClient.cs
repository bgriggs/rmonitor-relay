using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

class DesignHubClient : HubClient
{
    public DesignHubClient() : base(new DebugLoggerFactory(), new DesignConfiguration())
    {

    }
}

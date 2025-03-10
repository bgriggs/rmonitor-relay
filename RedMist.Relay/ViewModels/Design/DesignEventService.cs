using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

public class DesignEventService : EventService
{
    public DesignEventService() : base(new DesignEventManagementClient(), new DebugLoggerFactory())
    {
    }
}

using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

public class DesignEventManagementClient : EventManagementClient
{
    public DesignEventManagementClient() : base(new DesignConfiguration())
    {
    }
}

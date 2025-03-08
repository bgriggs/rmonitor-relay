using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

internal class DesignOrganizationClient : OrganizationClient
{
    public DesignOrganizationClient() : base(new DesignConfiguration())
    {
    }
}

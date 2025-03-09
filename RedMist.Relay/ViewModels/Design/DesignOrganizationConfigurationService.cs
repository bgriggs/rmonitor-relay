using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

public class DesignOrganizationConfigurationService : OrganizationConfigurationService
{
    public DesignOrganizationConfigurationService() : base(new DesignOrganizationClient(), new DebugLoggerFactory(), new DesignRelay())
    {
    }
}

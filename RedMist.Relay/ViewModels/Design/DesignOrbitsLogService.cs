using RedMist.Relay.Services;

namespace RedMist.Relay.ViewModels.Design;

public class DesignOrbitsLogService : OrbitsLogService
{
    public DesignOrbitsLogService() : base(new DesignOrganizationConfigurationService(), new DebugLoggerFactory(), new DesignConfiguration()) { }
}

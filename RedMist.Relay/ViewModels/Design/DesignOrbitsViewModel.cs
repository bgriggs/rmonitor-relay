namespace RedMist.Relay.ViewModels.Design;

public class DesignOrbitsViewModel : OrbitsViewModel
{
    public DesignOrbitsViewModel() : base(new DebugLoggerFactory(), new DesignOrganizationConfigurationService())
    {
    }
}

namespace RedMist.Relay.ViewModels.Design;

public class DesignX2ServerViewModel : X2ServerViewModel
{
    public DesignX2ServerViewModel() : base(new DesignOrganizationConfigurationService(), new DebugLoggerFactory(), new DesignConfiguration())
    {        
    }
}

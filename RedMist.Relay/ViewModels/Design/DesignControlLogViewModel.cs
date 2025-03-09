namespace RedMist.Relay.ViewModels.Design;

public class DesignControlLogViewModel : ControlLogViewModel
{
    public DesignControlLogViewModel() : base(new DebugLoggerFactory(), new DesignOrganizationConfigurationService(), new DesignOrganizationClient())
    {
    }
}

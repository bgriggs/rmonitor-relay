namespace RedMist.Relay.ViewModels.Design;

public class DesignMainViewModel : MainViewModel
{
    public DesignMainViewModel() : base(new DesignSettingsProvider(), new DesignRelay(), null!, new DesignConfiguration(), new DesignOrganizationClient(), 
        new DesignEventManagementClient(), new DebugLoggerFactory(), new DesignOrganizationConfigurationService())
    {
        // This is a design-time view model for the MainView.
        // It can be used to provide sample data or behavior for the view in the designer.
    }
}

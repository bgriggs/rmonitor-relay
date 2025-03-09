namespace RedMist.Relay.ViewModels.Design;

internal class DesignOrganizationViewModel : OrganizationViewModel
{
    public DesignOrganizationViewModel() : base(new DesignOrganizationConfigurationService(), new DesignSettingsProvider(), new DebugLoggerFactory())
    {
        OrgName = "Test Organization";
    }
}

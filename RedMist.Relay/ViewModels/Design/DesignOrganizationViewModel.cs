namespace RedMist.Relay.ViewModels.Design;

internal class DesignOrganizationViewModel : OrganizationViewModel
{
    public DesignOrganizationViewModel() : base(new DesignOrganizationClient(), new DesignSettingsProvider(), new DebugLoggerFactory())
    {
        OrgName = "Test Organization";
    }
}

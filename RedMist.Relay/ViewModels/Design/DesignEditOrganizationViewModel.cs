using RedMist.TimingCommon.Models.Configuration;

namespace RedMist.Relay.ViewModels.Design;

public class DesignEditOrganizationViewModel : EditOrganizationDialogViewModel
{
    public DesignEditOrganizationViewModel() : base(new Organization(), new DesignSettingsProvider())
    {
    }
}

using RedMist.TimingCommon.Models.Configuration;

namespace RedMist.Relay.Models;

public class OrganizationConfigurationChanged(Organization? organization)
{
    public Organization? Organization { get; } = organization;
}

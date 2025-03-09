using RedMist.TimingCommon.Models.Configuration;

namespace RedMist.Relay.Models;

public class OrganizationConnectionChanged(Organization organization)
{
    public Organization Organization { get; } = organization;
}

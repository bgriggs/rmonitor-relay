using RedMist.TimingCommon.Models.Configuration;

namespace RedMist.Relay.Models;

/// <summary>
/// Notification that the organization configuration has changed.
/// </summary>
/// <param name="organization">the new configuration</param>
public class OrganizationConfigurationChanged(Organization? organization)
{
    public Organization? Organization { get; } = organization;
}

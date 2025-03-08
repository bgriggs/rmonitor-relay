using CommunityToolkit.Mvvm.ComponentModel;
using RedMist.Relay.Services;
using RedMist.TimingCommon.Models.Configuration;
using System.ComponentModel.DataAnnotations;

namespace RedMist.Relay.ViewModels;

public partial class EditOrganizationDialogViewModel : ObservableValidator
{
    public Organization Organization { get; }

    private string clientId = string.Empty;
    [StringLength(255)]
    public string ClientId
    {
        get => clientId;
        set => SetProperty(ref clientId, value);
    }

    private string clientSecret = string.Empty;
    [StringLength(32)]
    public string ClientSecret
    {
        get => clientSecret;
        set => SetProperty(ref clientSecret, value);
    }

    private readonly ISettingsProvider settings;

    [StringLength(1024)]
    public string? OrgWebsite
    {
        get => Organization.Website;
        set => SetProperty(Organization.Website, value, Organization, (u, n) => u.Website = n, validate: true);
    }


    public EditOrganizationDialogViewModel(Organization organization, ISettingsProvider settings)
    {
        Organization = organization;
        this.settings = settings;
    }
}

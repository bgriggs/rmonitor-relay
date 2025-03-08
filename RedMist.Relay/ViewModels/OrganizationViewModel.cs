using CommunityToolkit.Mvvm.ComponentModel;
using DialogHostAvalonia;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using RedMist.Relay.Services;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class OrganizationViewModel : ObservableValidator
{
    private readonly OrganizationClient organizationClient;
    private readonly ISettingsProvider settings;
    private ILogger Logger { get; }
    private Organization? organization;

    [ObservableProperty]
    private string orgName = string.Empty;



    public OrganizationViewModel(OrganizationClient organizationClient, ISettingsProvider settings, ILoggerFactory loggerFactory)
    {
        this.organizationClient = organizationClient;
        this.settings = settings;
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }

    public async Task Initialize()
    {
        var clientId = settings.GetWithOverride("Keycloak:ClientId") ?? string.Empty;
        var clientSecret = settings.GetWithOverride("Keycloak:ClientSecret") ?? string.Empty;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            Logger.LogWarning("Missing client ID and/or secret to connect to cloud.");
            return;
        }

        organization = await organizationClient.LoadOrganizationAsync();
        if (organization != null)
        {
            OrgName = organization.Name;
        }
    }


    public async Task EditOrganization()
    {
        ObservableValidator vm;
        if (organization != null)
        {
            vm = new EditOrganizationDialogViewModel(organization, settings);
        }
        else
        {
            vm = new EditOrganizationDialogViewModel(new Organization(), settings);
        }

        var result = await DialogHost.Show(vm, "MainDialogHost");

        if (result is EditOrganizationDialogViewModel dvm)
        {
            await settings.SaveUser("Keycloak:ClientId", dvm.ClientId);
            await settings.SaveUser("Keycloak:ClientSecret", dvm.ClientSecret);

            var clientId = settings.GetWithOverride("Keycloak:ClientId") ?? string.Empty;
            var clientSecret = settings.GetWithOverride("Keycloak:ClientSecret") ?? string.Empty;

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                Logger.LogWarning("Missing client ID and/or secret save organization.");
                return;
            }

            try
            {
                await organizationClient.SaveOrganizationAsync(dvm.Organization);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save organization information.");
                var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
                {
                    ButtonDefinitions = [new ButtonDefinition { Name = "OK", IsDefault = true }],
                    ContentTitle = "Save Error",
                    ContentMessage = "Failed to save organization information: " + ex.Message,
                    Icon = Icon.Error,
                    MaxWidth = 500,
                });
                await box.ShowAsync();
            }
        }
    }
}

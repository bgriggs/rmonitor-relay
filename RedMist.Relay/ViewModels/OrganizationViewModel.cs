using Avalonia.Media.Imaging;
using Avalonia.Threading;
using BigMission.Shared.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DialogHostAvalonia;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using RedMist.Relay.Common;
using RedMist.Relay.Models;
using RedMist.Relay.Services;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class OrganizationViewModel : ObservableValidator, IRecipient<HubMessageStatistic>, IRecipient<ValueChangedMessage<HubConnectionState>>,
    IRecipient<OrganizationConfigurationChanged>
{
    private readonly OrganizationConfigurationService configurationService;
    private readonly ISettingsProvider settings;
    private ILogger Logger { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OrganizationLogo))]
    [NotifyPropertyChangedFor(nameof(HasOrganizationLogo))]
    private string orgName = string.Empty;

    [ObservableProperty]
    private int messagesSentToHub;

    [ObservableProperty]
    private string hubConnectionStr = "Disconnected";

    [ObservableProperty]
    private ConnectionState hubConnectionState = ConnectionState.Disconnected;

    public Bitmap? OrganizationLogo
    {
        get
        {
            var logo = configurationService.OrganizationConfiguration?.Logo;
            if (logo is not null)
            {
                using MemoryStream ms = new(logo);
                return Bitmap.DecodeToWidth(ms, 55);
            }
            return null;
        }
    }

    public bool HasOrganizationLogo => OrganizationLogo != null;

    private readonly Debouncer debouncer = new(TimeSpan.FromMilliseconds(500));


    public OrganizationViewModel(OrganizationConfigurationService configurationService, ISettingsProvider settings, ILoggerFactory loggerFactory)
    {
        this.configurationService = configurationService;
        this.settings = settings;
        Logger = loggerFactory.CreateLogger(GetType().Name);
        WeakReferenceMessenger.Default.RegisterAll(this);
    }


    public void Initialize()
    {
        var clientId = settings.GetWithOverride("Keycloak:ClientId") ?? string.Empty;
        var clientSecret = settings.GetWithOverride("Keycloak:ClientSecret") ?? string.Empty;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            Logger.LogWarning("Missing client ID and/or secret to connect to cloud.");
        }

        var organization = configurationService.OrganizationConfiguration;
        if (organization != null)
        {
            OrgName = organization.Name;
        }
    }

    public async Task EditOrganization()
    {
        EditOrganizationDialogViewModel vm;
        var organization = configurationService.OrganizationConfiguration;
        if (organization != null)
        {
            vm = new EditOrganizationDialogViewModel(organization, settings);
        }
        else
        {
            vm = new EditOrganizationDialogViewModel(new Organization(), settings);
        }
        vm.ClientId = settings.GetWithOverride("Keycloak:ClientId") ?? string.Empty;
        vm.ClientSecret = settings.GetWithOverride("Keycloak:ClientSecret") ?? string.Empty;

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
                await configurationService.SaveConfiguration(dvm.Organization);
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

    /// <summary>
    /// Receive the message from the hub that the connection state has changed.
    /// </summary>
    public void Receive(ValueChangedMessage<HubConnectionState> message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            HubConnectionStr = message.Value.ToString();

            if (message.Value == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connected)
            {
                HubConnectionState = ConnectionState.Connected;
            }
            else if (message.Value == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Disconnected)
            {
                HubConnectionState = ConnectionState.Disconnected;
            }
            else if (message.Value == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Reconnecting ||
                message.Value == Microsoft.AspNetCore.SignalR.Client.HubConnectionState.Connecting)
            {
                HubConnectionState = ConnectionState.Connecting;
            }
            else
            {
                HubConnectionState = ConnectionState.Unknown;
            }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Receive the message from the hub that the number of messages sent has changed.
    /// </summary>
    public void Receive(HubMessageStatistic message)
    {
        _ = debouncer.ExecuteAsync(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                MessagesSentToHub = message.Value;
            }, DispatcherPriority.Background);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Receive the organization configuration changed message.
    /// </summary>
    public void Receive(OrganizationConfigurationChanged message)
    {
        OrgName = configurationService.OrganizationConfiguration?.Name ?? string.Empty;
    }
}

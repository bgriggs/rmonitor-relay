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
using RedMist.Relay.Models;
using RedMist.Relay.Services;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class OrganizationViewModel : ObservableValidator, IRecipient<HubMessageStatistic>, IRecipient<ValueChangedMessage<HubConnectionState>>
{
    private readonly OrganizationClient organizationClient;
    private readonly ISettingsProvider settings;
    private ILogger Logger { get; }
    private Organization? organization;

    [ObservableProperty]
    private string orgName = string.Empty;

    [ObservableProperty]
    private int messagesSentToHub;

    [ObservableProperty]
    private string hubConnectionStr = "Disconnected";

    [ObservableProperty]
    private ConnectionState hubConnectionState = ConnectionState.Disconnected;

    private readonly Debouncer debouncer = new(TimeSpan.FromMilliseconds(500));


    public OrganizationViewModel(OrganizationClient organizationClient, ISettingsProvider settings, ILoggerFactory loggerFactory)
    {
        this.organizationClient = organizationClient;
        this.settings = settings;
        Logger = loggerFactory.CreateLogger(GetType().Name);
        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public async Task<Organization?> Initialize()
    {
        var clientId = settings.GetWithOverride("Keycloak:ClientId") ?? string.Empty;
        var clientSecret = settings.GetWithOverride("Keycloak:ClientSecret") ?? string.Empty;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            Logger.LogWarning("Missing client ID and/or secret to connect to cloud.");
            return null;
        }

        organization = await organizationClient.LoadOrganizationAsync();
        if (organization != null)
        {
            OrgName = organization.Name;
        }

        return organization;
    }

    public async Task EditOrganization()
    {
        EditOrganizationDialogViewModel vm;
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
}

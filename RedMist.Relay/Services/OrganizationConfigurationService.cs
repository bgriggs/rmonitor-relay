using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Models;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class OrganizationConfigurationService : BackgroundService, IRecipient<ValueChangedMessage<HubConnectionState>>
{
    private ILogger Logger { get; }

    private Organization? organizationConfiguration;
    public Organization? OrganizationConfiguration
    {
        get
        {
            lock (@lock)
            {
                return organizationConfiguration;
            }
        }
        private set
        {
            lock (@lock)
            {
                organizationConfiguration = value;
            }
            WeakReferenceMessenger.Default.Send(new OrganizationConfigurationChanged(OrganizationConfiguration));
        }
    }

    private readonly Lock @lock = new();
    private readonly OrganizationClient organizationClient;
    private readonly RelayService relay;


    public OrganizationConfigurationService(OrganizationClient organizationClient, ILoggerFactory loggerFactory, RelayService relay)
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.organizationClient = organizationClient;
        this.relay = relay;
        Logger = loggerFactory.CreateLogger(GetType().Name);
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            if (OrganizationConfiguration == null && relay.HubConnectionState == HubConnectionState.Connected)
            {
                await UpdateOrganizationConfiguration();
            }
        }
    }

    public void Receive(ValueChangedMessage<HubConnectionState> message)
    {
        if (message.Value == HubConnectionState.Connected)
        {
            Task.Run(UpdateOrganizationConfiguration);
        }
    }

    private async Task UpdateOrganizationConfiguration()
    {
        try
        {
            OrganizationConfiguration = await organizationClient.LoadOrganizationAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load organization configuration.");
        }
    }

    public async Task SaveConfiguration(Organization organization)
    {
        try
        {
            OrganizationConfiguration = organization;
            await organizationClient.SaveOrganizationAsync(organization);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save organization configuration.");
            throw;
        }
    }
}

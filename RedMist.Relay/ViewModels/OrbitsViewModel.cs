using Avalonia.Threading;
using BigMission.Shared.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using RedMist.Relay.Models;
using RedMist.Relay.Services;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class OrbitsViewModel : ObservableValidator, IRecipient<RMonitorMessageStatistic>, 
    IRecipient<OrbitsConnectionState>, IRecipient<OrganizationConfigurationChanged>
{
    private ILogger Logger { get; }

    [ObservableProperty]
    private int rmonitorMessagesReceived;

    [ObservableProperty]
    private string rmonitorConnectionStr = "Disconnected";

    [ObservableProperty]
    private ConnectionState rmonitorConnectionState = ConnectionState.Disconnected;

    private readonly Debouncer debouncer = new(TimeSpan.FromMilliseconds(500));
    private readonly OrganizationConfigurationService configurationService;
    [ObservableProperty]
    private ConnectionState orbitsLogsConnectionState = ConnectionState.Disconnected;


    public OrbitsViewModel(ILoggerFactory loggerFactory, OrganizationConfigurationService configurationService)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.configurationService = configurationService;
    }


    public void Initialize(Organization? organization = null)
    {
        try
        {
            organization ??= configurationService.OrganizationConfiguration;

            if (organization != null && organization.Orbits != null && !string.IsNullOrEmpty(organization.Orbits.LogsPath))
            {
                UpdateOrbitsLogsExist(organization.Orbits.LogsPath);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize organization information.");
        }
    }

    public async Task EditOrbits()
    {
        try
        {
            var organization = configurationService.OrganizationConfiguration;
            if (organization != null)
            {
                var orbitsConfig = organization.Orbits ?? new OrbitsConfiguration
                {
                    IP = "127.0.0.1",
                    Port = 50000,
                    LogsPath = "C:\\ProgramData\\MYLAPS\\Orbits 5.13.1\\Log"
                };

                var vm = new EditOrbitsDialogViewModel(orbitsConfig);
                var result = await DialogHost.Show(vm, "MainDialogHost");
                if (result is OrbitsConfiguration config)
                {
                    organization.Orbits = config;
                    await configurationService.SaveConfiguration(organization);
                    Logger.LogInformation("Orbits configuration updated.");

                    UpdateOrbitsLogsExist(config.LogsPath ?? string.Empty);
                }
            }
            else
            {
                MessageBoxManager.GetMessageBoxStandard("Organization Information", "Error loading Orbits data", ButtonEnum.Ok, Icon.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load organization information.");
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ButtonDefinitions = [new ButtonDefinition { Name = "OK", IsDefault = true }],
                ContentTitle = "Load Error",
                ContentMessage = "Failed to load organization information: " + ex.Message,
                Icon = Icon.Error,
                MaxWidth = 500,
            });
            await box.ShowAsync();
        }
    }

    public void Receive(RMonitorMessageStatistic message)
    {
        _ = debouncer.ExecuteAsync(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                RmonitorMessagesReceived = message.Value;
            }, DispatcherPriority.Background);
            return Task.CompletedTask;
        });
    }

    public void Receive(OrbitsConnectionState message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            RmonitorConnectionStr = message.Value.ToString();
            RmonitorConnectionState = message.Value;
        }, DispatcherPriority.Background);
    }

    public void Receive(OrganizationConfigurationChanged message)
    {
        Initialize(message.Organization);
    }

    public void UpdateOrbitsLogsExist(string path)
    {
        if (Directory.Exists(path))
        {
            OrbitsLogsConnectionState = ConnectionState.Connected;
        }
        else
        {
            OrbitsLogsConnectionState = ConnectionState.Disconnected;
        }
    }
}

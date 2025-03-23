using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
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
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class ControlLogViewModel : ObservableValidator, IRecipient<OrganizationConfigurationChanged>
{
    private ILogger Logger { get; }

    [ObservableProperty]
    private int logEntries;

    [ObservableProperty]
    private string logConnectionStr = "";

    [ObservableProperty]
    private ConnectionState logConnectionState = ConnectionState.Unknown;

    private IDisposable? checkUpdateControlLogStatisticsSubscription;
    private readonly OrganizationConfigurationService configurationService;
    private readonly OrganizationClient organizationClient;

    public ControlLogViewModel(ILoggerFactory loggerFactory, OrganizationConfigurationService configurationService, OrganizationClient organizationClient)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.configurationService = configurationService;
        this.organizationClient = organizationClient;
    }

    public void Initialize()
    {
        checkUpdateControlLogStatisticsSubscription ??= Observable.Interval(TimeSpan.FromSeconds(60)).Subscribe(async _ => { await CheckUpdateControlLogStatistics(); });
        
    }

    private async Task CheckUpdateControlLogStatistics()
    {
        try
        {
            var org = configurationService.OrganizationConfiguration;
            if (org != null && !string.IsNullOrEmpty(org.ControlLogType))
            {
                var stat = await organizationClient.LoadControlLogStatisticsAsync(org);

                Dispatcher.UIThread.Post(() =>
                {
                    if (stat.IsConnected)
                    {
                        LogEntries = stat.TotalEntries;
                        LogConnectionStr = ConnectionState.Connected.ToString();
                        LogConnectionState = ConnectionState.Connected;
                    }
                    else
                    {
                        LogEntries = 0;
                        LogConnectionStr = ConnectionState.Disconnected.ToString();
                        LogConnectionState = ConnectionState.Disconnected;
                    }
                }, DispatcherPriority.Background);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    LogEntries = 0;
                    LogConnectionStr = "N/A";
                    LogConnectionState = ConnectionState.Unknown;
                }, DispatcherPriority.Background);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update control log statistics");
        }
    }

    public async Task EditControlLog()
    {
        try
        {
            var organization = configurationService.OrganizationConfiguration;
            if (organization != null)
            {
                var cloneJson = JsonSerializer.Serialize(organization);
                var clone = JsonSerializer.Deserialize<Organization>(cloneJson)!;
                var vm = new EditControlLogDialogViewModel(clone);
                var result = await DialogHost.Show(vm, "MainDialogHost");
                if (result is Organization config)
                {
                    await configurationService.SaveConfiguration(config);
                    Logger.LogInformation("Control log configuration updated.");

                    _ = CheckUpdateControlLogStatistics();
                }
            }
            else
            {
                MessageBoxManager.GetMessageBoxStandard("Organization Information", "Error loading control log data", ButtonEnum.Ok, Icon.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save organization information.");
            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ButtonDefinitions = [new ButtonDefinition { Name = "OK", IsDefault = true }],
                ContentTitle = "Load Error",
                ContentMessage = "Failed to save organization information: " + ex.Message,
                Icon = Icon.Error,
                MaxWidth = 500,
            });
            await box.ShowAsync();
        }
    }

    public void Receive(OrganizationConfigurationChanged message)
    {
        _ = CheckUpdateControlLogStatistics();
    }
}
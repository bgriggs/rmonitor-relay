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
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class ControlLogViewModel : ObservableValidator
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
        checkUpdateControlLogStatisticsSubscription ??= Observable.Interval(TimeSpan.FromSeconds(10)).Subscribe(_ => CheckUpdateControlLogStatistics());
    }

    private void CheckUpdateControlLogStatistics()
    {
        try
        {

        }
        catch(Exception ex)
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

                    CheckUpdateControlLogStatistics();
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
}
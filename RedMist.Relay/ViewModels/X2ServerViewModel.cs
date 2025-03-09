using BigMission.Shared.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using Microsoft.Extensions.Configuration;
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

public partial class X2ServerViewModel : ObservableValidator
{
    private ILogger Logger { get; }

    [ObservableProperty]
    private int x2MessagesReceived;

    [ObservableProperty]
    private string x2ConnectionStr = "Disconnected";

    [ObservableProperty]
    private ConnectionState x2ConnectionState = ConnectionState.Disconnected;

    private readonly Debouncer debouncer = new(TimeSpan.FromMilliseconds(500));
    private readonly OrganizationConfigurationService configurationService;
    private readonly IConfiguration configuration;


    public X2ServerViewModel(OrganizationConfigurationService configurationService, ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.configurationService = configurationService;
        this.configuration = configuration;
    }


    public async Task EditX2()
    {
        try
        {
            var organization = configurationService.OrganizationConfiguration;
            if (organization != null)
            {
                var x2Config = organization.X2 ?? new X2Configuration
                {
                    Server = "192.168.0.10",
                    Username = "admin",
                    Password = "admin",
                };

                var vm = new EditX2ServerDialogViewModel(x2Config);
                var result = await DialogHost.Show(vm, "MainDialogHost");
                if (result is X2Configuration config)
                {
                    var key = configuration["AesKey"] ?? throw new ArgumentNullException("AesKey not found");
                    var encrypt = new EncryptionService(key, Consts.IV);
                    config.Password = encrypt.Encrypt(config.Password);
                    organization.X2 = config;
                    await configurationService.SaveConfiguration(organization);
                    Logger.LogInformation("Orbits configuration updated.");
                }
            }
            else
            {
                MessageBoxManager.GetMessageBoxStandard("Organization Information", "Error loading X2 data", ButtonEnum.Ok, Icon.Error);
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
}

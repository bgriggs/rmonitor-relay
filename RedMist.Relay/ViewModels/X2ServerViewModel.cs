using Avalonia.Threading;
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
using RedMist.Relay.Common;
using RedMist.Relay.Models;
using RedMist.Relay.Services;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class X2ServerViewModel : ObservableValidator, IRecipient<X2MessageStatistic>, IRecipient<X2ConnectionState>
{
    private ILogger Logger { get; }

    public bool IsSdkEnabled => x2Client.GetType() != typeof(NullX2Client);

    [ObservableProperty]
    private int x2MessagesReceived;

    [ObservableProperty]
    private string x2ConnectionStr = "Disconnected";

    [ObservableProperty]
    private ConnectionState x2ConnectionState = ConnectionState.Disconnected;

    private readonly Debouncer debouncer = new(TimeSpan.FromMilliseconds(200));
    private readonly OrganizationConfigurationService configurationService;
    private readonly IConfiguration configuration;
    private readonly IX2Client x2Client;


    public X2ServerViewModel(OrganizationConfigurationService configurationService, ILoggerFactory loggerFactory, 
        IConfiguration configuration, IX2Client x2Client)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
        WeakReferenceMessenger.Default.RegisterAll(this);
        this.configurationService = configurationService;
        this.configuration = configuration;
        this.x2Client = x2Client;
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
                    Logger.LogInformation("X2 configuration updated.");
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

    /// <summary>
    /// The connection the X2 server changed.
    /// </summary>
    public void Receive(X2ConnectionState message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            X2ConnectionState = message.State;
            X2ConnectionStr = message.State.ToString();
        }, DispatcherPriority.Background);
    }


    /// <summary>
    /// Update the messages received from the X2 server.
    /// </summary>
    /// <param name="message"></param>
    public void Receive(X2MessageStatistic message)
    {
        _ = debouncer.ExecuteAsync(() =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                X2MessagesReceived = message.MessagesReceived;
            }, DispatcherPriority.Background);
            return Task.CompletedTask;
        });
    }
}

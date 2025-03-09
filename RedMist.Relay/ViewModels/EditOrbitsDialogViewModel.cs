using CommunityToolkit.Mvvm.ComponentModel;
using DialogHostAvalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public class EditOrbitsDialogViewModel : ObservableValidator
{
    public OrbitsConfiguration Configuration { get; }

    [CustomValidation(typeof(EditOrbitsDialogViewModel), nameof(IpValidate))]
    public string Ip
    {
        get => Configuration.IP;
        set => SetProperty(Configuration.IP, value, Configuration, (u, n) => u.IP = n, validate: true);
    }

    [Range(1, 65535)]
    public int Port
    {
        get => Configuration.Port;
        set => SetProperty(Configuration.Port, value, Configuration, (u, n) => u.Port = n, validate: true);
    }

    [StringLength(2048)]
    public string LogsPath
    {
        get => Configuration.LogsPath ?? string.Empty;
        set => SetProperty(Configuration.LogsPath, value, Configuration, (u, n) => u.LogsPath = n, validate: true);
    }


    public EditOrbitsDialogViewModel(OrbitsConfiguration configuration)
    {
        Configuration = configuration;
    }


    public static ValidationResult IpValidate(string host, ValidationContext context)
    {
        if (IPAddress.TryParse(host, out _))
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult("IP Address is not valid");
    }

    public async Task Save()
    {
        if (HasErrors)
        {
            var errors = GetErrors();
            var sb = new StringBuilder();
            foreach (var e in errors)
            {
                sb.AppendLine(e.ErrorMessage);
            }

            var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ButtonDefinitions = [new ButtonDefinition { Name = "OK", IsDefault = true }],
                ContentTitle = "Validation Errors",
                ContentMessage = "Please correct the following errors:" + Environment.NewLine + sb.ToString(),
                Icon = Icon.Error,
                MaxWidth = 500,
            });
            await box.ShowAsync();
        }
        else
        {
            Configuration.IP = Ip;
            Configuration.Port = Port;
            Configuration.LogsPath = LogsPath;
            DialogHost.Close("MainDialogHost", Configuration);
        }
    }
}

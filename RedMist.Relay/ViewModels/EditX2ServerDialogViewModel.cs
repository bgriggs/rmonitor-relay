using CommunityToolkit.Mvvm.ComponentModel;
using DialogHostAvalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using RedMist.TimingCommon.Models.Configuration;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class EditX2ServerDialogViewModel : ObservableValidator
{
    public X2Configuration Configuration { get; }

    [StringLength(128)]
    public string Server
    {
        get => Configuration.Server ?? string.Empty;
        set => SetProperty(Configuration.Server, value, Configuration, (u, n) => u.Server = n, validate: true);
    }

    [StringLength(128)]
    public string Username
    {
        get => Configuration.Username ?? string.Empty;
        set => SetProperty(Configuration.Username, value, Configuration, (u, n) => u.Username = n, validate: true);
    }

    [StringLength(30)]
    public string Password
    {
        get => Configuration.Password ?? string.Empty;
        set => SetProperty(Configuration.Password, value, Configuration, (u, n) => u.Password = n, validate: true);
    }


    public EditX2ServerDialogViewModel(X2Configuration configuration)
    {
        Configuration = configuration;
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
            DialogHost.Close("MainDialogHost", Configuration);
        }
    }
}

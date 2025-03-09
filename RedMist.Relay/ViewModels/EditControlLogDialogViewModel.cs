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
using System.Text;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class EditControlLogDialogViewModel : ObservableValidator
{
    [ObservableProperty]
    private ControlLogTypeViewModel selectedControlLogType;

    public ControlLogTypeViewModel[] ControlLogTypes { get; } = [
        new ControlLogTypeViewModel("None", string.Empty),
        new ControlLogTypeViewModel("WRL Google Sheet", "WrlGoogleSheet")];

    public Organization Organization { get; private set; }

    public EditControlLogDialogViewModel(Organization organization)
    {
        Organization = organization;

        SelectedControlLogType = ControlLogTypes.FirstOrDefault(l => l.Value == Organization.ControlLogType) ?? ControlLogTypes[0];

        if (SelectedControlLogType.Value == "WrlGoogleSheet")
        {
            var parts = Organization.ControlLogParams?.Split(';');
            if (parts != null && parts.Length == 2)
            {
                SelectedControlLogType.WrlGoogleSheetId = parts[0];
                SelectedControlLogType.WrlGoogleTabName = parts[1];
            }
        }
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
            Organization.ControlLogType = SelectedControlLogType.Value;
            if (SelectedControlLogType.Value == "WrlGoogleSheet")
            {
                Organization.ControlLogParams = $"{SelectedControlLogType.WrlGoogleSheetId};{SelectedControlLogType.WrlGoogleTabName}";
            }
            else // None
            {
                Organization.ControlLogParams = string.Empty;
            }

            DialogHost.Close("MainDialogHost", Organization);
        }
    }
}

public partial class ControlLogTypeViewModel(string name, string value) : ObservableValidator
{
    public string Name { get; set; } = name;
    public string Value { get; set; } = value;

    [StringLength(80)]
    [ObservableProperty]
    private string wrlGoogleSheetId = string.Empty;

    [StringLength(200)]
    [ObservableProperty]
    private string wrlGoogleTabName = string.Empty;
}

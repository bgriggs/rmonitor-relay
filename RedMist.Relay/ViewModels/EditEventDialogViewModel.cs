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

public partial class EditEventDialogViewModel(Event model) : ObservableValidator
{
    public Event Model { get; } = model;

    [Required]
    [StringLength(512, MinimumLength = 5)]
    public string Name
    {
        get => Model.Name ?? string.Empty;
        set => SetProperty(Model.Name, value, Model, (u, n) => u.Name = n, validate: true);
    }

    [Required]
    public DateTime StartDate
    {
        get => Model.StartDate;
        set
        {
            if (SetProperty(Model.StartDate, value, Model, (u, n) => u.StartDate = n, validate: true))
            {
                ValidateProperty(EndDate, nameof(EndDate));
            }
        }
    }

    [Required]
    [CustomValidation(typeof(EditEventDialogViewModel), nameof(ValidateEndDateAfterStartDate))]
    public DateTime EndDate
    {
        get => Model.EndDate;
        set => SetProperty(Model.EndDate, value, Model, (u, n) => u.EndDate = n, validate: true);
    }

    [Url]
    [StringLength(512)]
    public string EventUrl
    {
        get => Model.EventUrl ?? string.Empty;
        set => SetProperty(Model.EventUrl, value, Model, (u, n) => u.EventUrl = n, validate: true);
    }

    [StringLength(128)]
    public string TrackName
    {
        get => Model.TrackName ?? string.Empty;
        set => SetProperty(Model.TrackName, value, Model, (u, n) => u.TrackName = n, validate: true);
    }

    [StringLength(64)]
    public string CourseConfiguration
    {
        get => Model.CourseConfiguration ?? string.Empty;
        set => SetProperty(Model.CourseConfiguration, value, Model, (u, n) => u.CourseConfiguration = n, validate: true);
    }

    [StringLength(20)]
    public string Distance
    {
        get => Model.Distance ?? string.Empty;
        set => SetProperty(Model.Distance, value, Model, (u, n) => u.Distance = n, validate: true);
    }

    [StringLength(128)]
    public string BroadcastCompanyName
    {
        get => Model.Broadcast.CompanyName ?? string.Empty;
        set => SetProperty(Model.Broadcast.CompanyName, value, Model, (u, n) => u.Broadcast.CompanyName = n, validate: true);
    }

    [Url]
    [StringLength(512)]
    public string BroadcastUrl
    {
        get => Model.Broadcast.Url ?? string.Empty;
        set => SetProperty(Model.Broadcast.Url, value, Model, (u, n) => u.Broadcast.Url = n, validate: true);
    }

    public bool AllowDelete => Model.Id > 0;


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
            DialogHost.Close("MainDialogHost", Model);
        }
    }

    public async Task Delete()
    {
        var box = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ButtonDefinitions = [new ButtonDefinition { Name = "Yes", IsDefault = true }, new ButtonDefinition { Name = "No" }],
            ContentTitle = "Delete Event",
            ContentMessage = $"Are you sure you want to delete the event '{Model.Name}'?",
            Icon = Icon.Warning,
            MaxWidth = 500,
        });
        var result = await box.ShowAsync();
        if (result == "Yes")
        {
            DialogHost.Close("MainDialogHost", true);
        }
    }

    public static ValidationResult ValidateEndDateAfterStartDate(DateTime endDate, ValidationContext context)
    {
        var instance = context.ObjectInstance as EditEventDialogViewModel;
        if (instance != null && endDate <= instance.StartDate)
        {
            return new ValidationResult("End date must be after start date.");
        }
        return ValidationResult.Success!;
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DialogHostAvalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using RedMist.TimingCommon.Models.Configuration;
using RedMist.TimingCommon.Models.X2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class EditEventDialogViewModel : ObservableValidator
{
    private Event model;
    public Event Model
    {
        get => model;
        protected set
        {
            model = value;
            InitializeSchedule(model.Schedule.Entries, Schedule, StartDate, EndDate);
        }
    }

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

    #region Broadcast

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

    #endregion

    #region Schedule
    
    public ObservableCollection<DayScheduleViewModel> Schedule { get; } = [];

    #endregion

    #region X2

    public ObservableCollection<X2LoopViewModel> Loops { get; } = [];

    #endregion

    public bool AllowDelete => Model.Id > 0;

    

    public EditEventDialogViewModel(Event model)
    {
        this.model = model;
        Model = model;
    }


    public async Task Save()
    {
        var errors = GetAllErrors();
        if (errors.Any())
        {
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
            Model.Schedule.Entries = [.. Schedule.SelectMany(e => e.EntryViewModels.Select(s => s.Model))];
            Model.LoopsMetadata = [.. Loops.Select(l => l.LoopMetadata)];
            DialogHost.Close("MainDialogHost", Model);
        }
    }

    private IEnumerable<ValidationResult> GetAllErrors()
    {
        List<ValidationResult> errors = [];
        var eventVmErrors = GetErrors();
        if (eventVmErrors.Any())
        {
            errors.AddRange(eventVmErrors);
        }

        // Validate Schedule
        foreach (var day in Schedule)
        {
            foreach (var entry in day.EntryViewModels)
            {
                var entryErrors = entry.GetErrors();
                if (entryErrors.Any())
                {
                    errors.AddRange(entryErrors);
                }
            }
        }

        // Validate Loops
        foreach (var loop in Loops)
        {
            var loopErrors = loop.GetErrors();
            if (loopErrors.Any())
            {
                errors.AddRange(loopErrors);
            }
        }

        return errors;
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

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(StartDate) || e.PropertyName == nameof(EndDate))
        {
            InitializeSchedule(model.Schedule.Entries, Schedule, StartDate, EndDate);
        }
    }

    public static void InitializeSchedule(List<EventScheduleEntry> entries, ObservableCollection<DayScheduleViewModel> schedule, DateTime start, DateTime end)
    {
        if ((end > start) && (end - start < TimeSpan.FromDays(5)))
        {
            schedule.Clear();
            var currentDay = start.Date;
            while (currentDay <= end.Date)
            {
                // select entries by first day, second day, etc. rather than exact date?
                var dayEntries = entries.Where(x => x.DayOfEvent.Date == currentDay).OrderBy(x => x.StartTime).ToList();
                schedule.Add(new DayScheduleViewModel(currentDay, dayEntries));
                currentDay = currentDay.AddDays(1);
            }
        }
        else
        {
            // Log when the schedule is too long
        }
    }

    public void InitializeLoops(List<Loop> loops)
    {
        foreach (var loop in loops.OrderBy(l => l.Order))
        {
            var metadata = Model.LoopsMetadata.FirstOrDefault(x => x.Id == loop.Id);
            if (metadata == null)
            {
                metadata = new LoopMetadata { Id = loop.Id, Type = LoopType.Other, Name = loop.Name, EventId = Model.Id };
                Model.LoopsMetadata.Add(metadata);
            }

            var vm = new X2LoopViewModel(metadata)
            {
                Id = loop.Id,
                X2Name = loop.Name,
            };
            Loops.Add(vm);
        }
    }
}

public partial class DayScheduleViewModel : ObservableValidator, IRecipient<DeleteScheduleDayEntryCommand>
{
    private readonly DateTime day;
    public string DayString { get; }
    public ObservableCollection<DayScheduleEntryViewModel> EntryViewModels { get; } = [];

    public DayScheduleViewModel(DateTime day, List<EventScheduleEntry> dayEntries)
    {
        foreach (var e in dayEntries.OrderBy(x => x.StartTime))
        {
            EntryViewModels.Add(new DayScheduleEntryViewModel(e));
        }

        var dtf = new CultureInfo("en-US", false).DateTimeFormat;
        dtf.DayNames = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];
        DayString = dtf.GetDayName(day.DayOfWeek) + ", " + day.ToString("MM/dd");
        this.day = day;

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    public void AddEntry()
    {
        var entryVm = new DayScheduleEntryViewModel(new EventScheduleEntry
        {
            DayOfEvent = day,
            StartTime = new DateTime(1, 1, 1, 9, 0, 0),
            EndTime = new DateTime(1, 1, 1, 17, 0, 0),
            Name = string.Empty
        });

        if (EntryViewModels.Any())
        {
            var lastEntry = EntryViewModels.Last();
            entryVm.Model.StartTime = lastEntry.Model.EndTime;
            entryVm.Model.EndTime = entryVm.Model.StartTime.Add(TimeSpan.FromHours(1));
            entryVm.InitializeTime();
        }

        EntryViewModels.Add(entryVm);
    }

    /// <summary>
    /// Delete row.
    /// </summary>
    /// <param name="message">row to delete</param>
    public void Receive(DeleteScheduleDayEntryCommand message)
    {
        EntryViewModels.Remove(message.Vm);
    }
}

public partial class DayScheduleEntryViewModel : ObservableValidator
{
    public EventScheduleEntry Model { get; }

    [Required]
    [StringLength(60, MinimumLength = 3)]
    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (u, n) => u.Name = n, validate: true);
    }

    private string startTimeStr = string.Empty;
    [CustomValidation(typeof(DayScheduleEntryViewModel), nameof(ValidateEndTimeAfterStartTime))]
    [CustomValidation(typeof(DayScheduleEntryViewModel), nameof(ValidateTime))]
    public string StartTime
    {
        get => startTimeStr;
        set
        {
            if (SetProperty(Model.StartTime.ToString("h:mm tt"), value, Model, (u, n) => { DateTime.TryParseExact(n, "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt); u.StartTime = dt; }, validate: true))
            {
                startTimeStr = value;
                ValidateProperty(EndTime, nameof(EndTime));
            }
        }
    }

    private string endTimeStr = string.Empty;
    [Required]
    public string EndTime
    {
        get => endTimeStr;
        set
        {
            if (SetProperty(Model.EndTime.ToString("h:mm tt"), value, Model, (u, n) => { DateTime.TryParseExact(n, "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt); u.EndTime = dt; }, validate: true))
            {
                endTimeStr = value;
            }
        }
    }


    public DayScheduleEntryViewModel(EventScheduleEntry model)
    {
        Model = model;
        InitializeTime();
    }


    public void InitializeTime()
    {
        startTimeStr = Model.StartTime.ToString("h:mm tt");
        endTimeStr = Model.EndTime.ToString("h:mm tt");
    }

    public static ValidationResult ValidateTime(string time, ValidationContext context)
    {
        if (DateTime.TryParseExact(time, "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            return ValidationResult.Success!;
        }

        return new ValidationResult("Time must be in format: hh:mm tt");
    }

    public static ValidationResult ValidateEndTimeAfterStartTime(DateTime endDate, ValidationContext context)
    {
        var instance = context.ObjectInstance as EditEventDialogViewModel;
        if (instance != null && endDate.TimeOfDay <= instance.StartDate.TimeOfDay)
        {
            return new ValidationResult("End time must be after start time.");
        }
        return ValidationResult.Success!;
    }

    public void DeleteEntry(object row)
    {
        if (row is DayScheduleEntryViewModel vm)
        {
            WeakReferenceMessenger.Default.Send(new DeleteScheduleDayEntryCommand(vm));
        }
    }
}

public class DeleteScheduleDayEntryCommand(DayScheduleEntryViewModel vm)
{
    public DayScheduleEntryViewModel Vm { get; } = vm;
}

public partial class X2LoopViewModel : ObservableValidator
{
    public LoopMetadata LoopMetadata { get; private set; }
    public uint Id { get; set; }
    public string X2Name { get; set; } = string.Empty;

    public static LoopTypeViewModel[] LoopTypes
    {
        get
        {
            return [new LoopTypeViewModel(LoopType.StartFinish, "Start / Finish"),
                    new LoopTypeViewModel(LoopType.PitIn, "Pit Entrance"),
                    new LoopTypeViewModel(LoopType.PitExit, "Pit Exit"),
                    new LoopTypeViewModel(LoopType.PitStartFinish, "Pit Start / Finish"),
                    new LoopTypeViewModel(LoopType.PitOther, "Pit (other)"),
                    new LoopTypeViewModel(LoopType.Other, "Other / Sector"),];
        }
    }

    //private LoopTypeViewModel selectedLoopType;
    //public LoopTypeViewModel SelectedLoopType
    //{
    //    get => selectedLoopType;
    //    set
    //    {
    //        if (SetProperty(ref selectedLoopType, value))
    //        {
    //            loopMetadata.Type = value.Type;
    //        }
    //    }
    //}

    public int index;
    public int SelectedIndex
    {
        get { return index; }
        set
        {
            if (SetProperty(ref index, value))
            {
                LoopMetadata.Type = (LoopType)value;
            }
        }
    }

    public string TypeName => LoopTypes[index].Name;

    [StringLength(14)]
    public string Name
    {
        get => LoopMetadata.Name;
        set => SetProperty(LoopMetadata.Name, value, LoopMetadata, (u, n) => u.Name = n, validate: true);
    }

    public X2LoopViewModel(LoopMetadata loopMetadata)
    {
        LoopMetadata = loopMetadata;
        index = (int)loopMetadata.Type;
    }
}

public class LoopTypeViewModel(LoopType type, string name)
{
    public LoopType Type { get; set; } = type;
    public string Name { get; } = name;
}

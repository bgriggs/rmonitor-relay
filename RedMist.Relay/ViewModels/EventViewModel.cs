using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DialogHostAvalonia;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using RedMist.Relay.Common;
using RedMist.Relay.Services;
using RedMist.TimingCommon.Models.Configuration;
using RedMist.TimingCommon.Models.X2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RedMist.Relay.ViewModels;

public partial class EventViewModel : ObservableValidator, IRecipient<ValueChangedMessage<HubConnectionState>>
{
    private ILogger Logger { get; }
    private readonly EventService eventService;
    private readonly IX2Client x2Client;
    private readonly ObservableCollection<EventSummary> eventSummaries = [];
    public ObservableCollection<EventSummary> EventSummaries => eventSummaries;

    private EventSummary? selectedEvent;
    public EventSummary? SelectedEvent
    {
        get => selectedEvent;
        set
        {
            SetProperty(ref selectedEvent, value);
            UpdateActiveEvent(selectedEvent?.Id);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DateRange))]
    [NotifyPropertyChangedFor(nameof(EventUrl))]
    [NotifyPropertyChangedFor(nameof(TrackInfo))]
    [NotifyPropertyChangedFor(nameof(IsBroadcast))]
    [NotifyPropertyChangedFor(nameof(BroadcastCompanyName))]
    [NotifyPropertyChangedFor(nameof(BroadcastUrl))]
    [NotifyPropertyChangedFor(nameof(IsBroadcastUrlEnabled))]
    [NotifyPropertyChangedFor(nameof(IsSchedule))]
    private Event? eventData;

    public string DateRange
    {
        get
        {
            if (EventData == null)
                return "Date: N/A";
            return $"Date: {EventData?.StartDate.ToString("MM/dd/yyyy")} - {EventData?.EndDate.ToString("MM/dd/yyyy")}";
        }
    }

    public string EventUrl => EventData?.EventUrl ?? string.Empty;
    public string TrackInfo => $"{EventData?.TrackName} - {EventData?.CourseConfiguration} - {EventData?.Distance}";
    public bool IsBroadcast => !string.IsNullOrEmpty(EventData?.Broadcast?.Url) || !string.IsNullOrEmpty(EventData?.Broadcast?.CompanyName);
    public string BroadcastCompanyName => $"Company: {EventData?.Broadcast?.CompanyName ?? string.Empty}";
    public string BroadcastUrl => EventData?.Broadcast?.Url ?? string.Empty;
    public bool IsBroadcastUrlEnabled => !string.IsNullOrEmpty(EventData?.Broadcast?.Url);
    public bool IsSchedule => EventData?.Schedule?.Entries?.Count > 0;
    public ObservableCollection<DayScheduleViewModel> Schedule { get; } = [];
    public ObservableCollection<X2LoopViewModel> X2Loops { get; } = [];

    private bool isInitialized = false;


    public EventViewModel(EventService eventService, ILoggerFactory loggerFactory, IX2Client x2Client)
    {
        Logger = loggerFactory.CreateLogger(GetType().Name);
        this.eventService = eventService;
        this.x2Client = x2Client;
        WeakReferenceMessenger.Default.RegisterAll(this);
    }


    /// <summary>
    /// Load the event summaries and set the active event.
    /// </summary>
    public async Task Initialize()
    {
        var summaries = await eventService.LoadEventSummariesAsync();
        eventSummaries.Clear();

        foreach (var summary in summaries.OrderByDescending(s => s.StartDate))
        {
            eventSummaries.Add(summary);
        }

        SelectedEvent = eventSummaries.FirstOrDefault(s => s.IsActive);
    }

    /// <summary>
    /// As user selects event in the combo box, make it the event that the system will use
    /// to associate data received from the timing systems.
    /// </summary>
    private async void UpdateActiveEvent(int? eventId)
    {
        if (eventId is null)
        {
            EventData = null;
        }
        else
        {
            try
            {
                EventData = await eventService.UpdateEventActiveAndLoadAsync(eventId.Value);
                Dispatcher.UIThread.Post(RefreshSchedule);
                Dispatcher.UIThread.Post(RefreshX2Loops);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to update active event.");
                Dispatcher.UIThread.Post(() =>
                    MessageBoxManager.GetMessageBoxStandard("Error", "Failed to update active event.", ButtonEnum.Ok, Icon.Error));
            }
        }
    }

    private void RefreshSchedule()
    {
        Schedule.Clear();
        if (EventData != null)
        {
            EditEventDialogViewModel.InitializeSchedule(EventData.Schedule.Entries, Schedule, EventData.StartDate, EventData.EndDate);
        }
    }

    private void RefreshX2Loops()
    {
        X2Loops.Clear();
        if (EventData != null)
        {
            foreach (var loop in EventData.LoopsMetadata)
            {
                X2Loops.Add(new X2LoopViewModel(loop));
            }
        }
    }

    public async Task EditEvent()
    {
        if (EventData == null)
        {
            MessageBoxManager.GetMessageBoxStandard("Missing Event", "There is no event selected to edit.", ButtonEnum.Ok, Icon.Error);
            return;
        }

        var cloneJson = JsonSerializer.Serialize(EventData);
        var clone = JsonSerializer.Deserialize<Event>(cloneJson)!;

        var vm = new EditEventDialogViewModel(clone);
        var loops = TryGetX2Loops();
        if (loops.Count > 0)
        {
            vm.InitializeLoops(loops);
        }

        var result = await DialogHost.Show(vm, "MainDialogHost");
        if (result is Event updatedEvent)
        {
            try
            {
                EventData = updatedEvent;
                RefreshSchedule();
                RefreshX2Loops();
                await eventService.UpdateEventAsync(updatedEvent);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save event.");
                MessageBoxManager.GetMessageBoxStandard("Error", "Failed to save event.", ButtonEnum.Ok, Icon.Error);
            }
        }
        else if (result is bool delete && delete)
        {
            await eventService.DeleteEventAsync(EventData.Id);
            await Initialize();
        }
    }

    public async Task AddEvent()
    {
        var @event = new Event
        {
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddDays(1)
        };
        var vm = new EditEventDialogViewModel(@event);
        var loops = TryGetX2Loops();
        if (loops.Count > 0)
        {
            vm.InitializeLoops(loops);
        }

        var result = await DialogHost.Show(vm, "MainDialogHost");
        if (result is Event updatedEvent)
        {
            try
            {
                var eventId = await eventService.SaveNewEventAsync(updatedEvent);
                // Set as the active event
                await eventService.UpdateEventActiveAndLoadAsync(eventId);
                // Reload the event list
                await Initialize();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to save new event.");
                MessageBoxManager.GetMessageBoxStandard("Error", "Failed to save new event.", ButtonEnum.Ok, Icon.Error);
            }
        }
    }

    private List<Loop> TryGetX2Loops()
    {
        try
        {
            return x2Client.GetLoops() ?? [];
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get X2 loops.");
            return [];
        }
    }

    public async void Receive(ValueChangedMessage<HubConnectionState> message)
    {
        if (!isInitialized && message.Value == HubConnectionState.Connected)
        {
            try
            {
                await Initialize();
                isInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to initialize event view model.");
            }
        }
    }

    public void OpenEventUrl()
    {
        if (EventData == null || string.IsNullOrEmpty(EventData.EventUrl))
        {
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = EventData.EventUrl,
            UseShellExecute = true
        });
    }

    public void OpenBroadcastUrl()
    {
        if (EventData == null || EventData.Broadcast == null || string.IsNullOrEmpty(EventData.Broadcast.Url))
        {
            return;
        }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = EventData.Broadcast.Url,
            UseShellExecute = true
        });
    }
}

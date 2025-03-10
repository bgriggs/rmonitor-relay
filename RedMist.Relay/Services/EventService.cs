using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedMist.Relay.Models;
using RedMist.TimingCommon.Models.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedMist.Relay.Services;

public class EventService : BackgroundService
{
    private ILogger Logger { get; }

    private Event? @event;
    public Event? Event
    {
        get
        {
            lock (@lock)
            {
                return @event;
            }
        }
        private set
        {
            lock (@lock)
            {
                @event = value;
            }
            WeakReferenceMessenger.Default.Send(new EventConfigurationChanged(@event));
        }
    }

    private readonly Lock @lock = new();
    private readonly EventManagementClient eventManagementClient;


    public EventService(EventManagementClient eventManagementClient, ILoggerFactory loggerFactory)
    {
        WeakReferenceMessenger.Default.RegisterAll(this);
        Logger = loggerFactory.CreateLogger(GetType().Name);
        this.eventManagementClient = eventManagementClient;
    }


    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public async Task<Event?> UpdateEventActiveAndLoadAsync(int eventId)
    {
        var loadTask = eventManagementClient.LoadEventAsync(eventId);
        await eventManagementClient.UpdateEventStatusActiveAsync(eventId);
        var e = await loadTask;
        Event = e;
        return e;
    }

    public async Task UpdateEventAsync(Event e)
    {
        await eventManagementClient.UpdateEventAsync(e);
    }

    public async Task<int> SaveNewEventAsync(Event e)
    {
        return await eventManagementClient.SaveNewEventAsync(e);
    }

    public async Task<List<EventSummary>> LoadEventSummariesAsync()
    {
        return await eventManagementClient.LoadEventSummariesAsync();
    }

    public async Task DeleteEventAsync(int eventId)
    {
        await eventManagementClient.DeleteEventAsync(eventId);
    }
}

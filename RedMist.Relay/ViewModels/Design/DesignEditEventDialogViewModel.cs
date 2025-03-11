using RedMist.TimingCommon.Models.Configuration;
using System;

namespace RedMist.Relay.ViewModels.Design;

public class DesignEditEventDialogViewModel : EditEventDialogViewModel
{
    public DesignEditEventDialogViewModel() : base(new Event())
    {
        var eventData = new Event
        {
            Name = "Design Event",
            StartDate = DateTime.Now.AddDays(-1),
            EndDate = DateTime.Now.AddDays(1),
            TrackName = "Design Track",
            EventUrl = "http://example.com/event",
            CourseConfiguration = "FULL",
            Distance = "3.3 mi",
            Broadcast = new BroadcasterConfig
            {
                Url = "http://example.com/broadcast",
                CompanyName = "Design Company"
            },
            Schedule = new EventSchedule
            {
                Entries =
                [
                    new EventScheduleEntry { Name = "Entry 1", DayOfEvent = DateTime.Now.AddDays(-1), StartTime = new DateTime(1, 3, 9, 9, 0, 0), EndTime = new DateTime(1, 1, 1, 17, 0, 0) },
                    new EventScheduleEntry { Name = "Entry 2", DayOfEvent = DateTime.Now, StartTime = new DateTime(1, 1, 1, 8, 0, 0), EndTime = new DateTime(1, 1, 1, 15, 0, 0) }
                ]
            }
        };
        Model = eventData;
    }
}

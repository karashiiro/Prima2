using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Http;
using Google.Apis.Services;

namespace Prima.Application.Scheduling.Calendar;

public class GoogleCalendarClient
{
    private readonly CalendarService _service;

    public GoogleCalendarClient(IConfigurableHttpClientInitializer? credential)
    {
        _service = new CalendarService(new BaseClientService.Initializer
        {
            ApplicationName = "Prima",
            HttpClientInitializer = credential,
        });
    }

    public async Task<IList<Event>> ListEvents(string calendarId, DateTime? after)
    {
        var listRequest = _service.Events.List(calendarId);
        listRequest.ShowDeleted = false;
        listRequest.SingleEvents = true;
        listRequest.TimeMin = after;
        listRequest.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        var events = await listRequest.ExecuteAsync();
        return events.Items;
    }

    public async Task<Event> CreateEvent(string calendarId, string title, string description, DateTime startTime,
        DateTime endTime)
    {
        var @event = new Event
        {
            Summary = title,
            Description = description,
            Start = new EventDateTime { DateTime = startTime, TimeZone = "Africa/Accra" },
            End = new EventDateTime { DateTime = endTime, TimeZone = "Africa/Accra" },
        };
        var createRequest = _service.Events.Insert(@event, calendarId);
        return await createRequest.ExecuteAsync();
    }

    public async Task<Event> UpdateEvent(string calendarId, string eventId, string? title, string? description,
        DateTime? startTime, DateTime? endTime)
    {
        var @event = new Event
        {
            Summary = title,
            Description = description,
            Start = new EventDateTime { DateTime = startTime, TimeZone = "Africa/Accra" },
            End = new EventDateTime { DateTime = endTime, TimeZone = "Africa/Accra" },
        };
        var updateRequest = _service.Events.Update(@event, calendarId, eventId);
        return await updateRequest.ExecuteAsync();
    }

    public async Task<Event?> GetEvent(string calendarId, string eventId)
    {
        var getRequest = _service.Events.Get(calendarId, eventId);
        return await getRequest.ExecuteAsync();
    }

    public async Task DeleteEvent(string calendarId, string eventId)
    {
        var deleteRequest = _service.Events.Delete(calendarId, eventId);
        await deleteRequest.ExecuteAsync();
    }
}
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Google.Apis.Calendar.v3.EventsResource;

namespace Prima.Scheduler.Services
{
    public sealed class SchedulingService : IDisposable
    {
        private static readonly string[] _scopes = { CalendarService.Scope.CalendarEvents };
        private const string _applicationName = "Prima";

        private readonly CalendarService _service;

        private string GCredentialsFile { get => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "credentials.json" // Only use Windows for testing.
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "credentials.json"); }
        private string GTokenFile { get => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "token.json"
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "token.json"); }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public SchedulingService()
        {
            using var stream = new FileStream(GCredentialsFile, FileMode.Open, FileAccess.Read);
            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                _scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(GTokenFile, true)
            ).Result;
            Log.Information("Credential file saved to {File}", GTokenFile);

            _service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = _applicationName,
            });
        }

        public async Task<IList<Event>> GetEvents(string calendarId)
        {
            ListRequest request = _service.Events.List(calendarId);
            request.TimeMin = DateTime.Now;
            request.ShowDeleted = false;
            request.SingleEvents = true;
            request.OrderBy = ListRequest.OrderByEnum.StartTime;
            return (await request.ExecuteAsync()).Items;
        }

        public async Task AddEvent(string calendarId, Event eventItem) {
            InsertRequest request = _service.Events.Insert(eventItem, calendarId);
            await request.ExecuteAsync();
        }

        public async Task DeleteEvent(string calendarId, string eventId)
        {
            DeleteRequest request = _service.Events.Delete(calendarId, eventId);
            await request.ExecuteAsync();
        }

        public async Task UpdateEvent(string calendarId, string eventId, Event eventItem)
        {
            UpdateRequest request = _service.Events.Update(eventItem, calendarId, eventId);
            await request.ExecuteAsync();
        }

        public async Task PatchEvent(string calendarId, string eventId, Event partialEventItem)
        {
            PatchRequest request = _service.Events.Patch(partialEventItem, calendarId, eventId);
            await request.ExecuteAsync();
        }

        public void Dispose()
        {
            _service.Dispose();
        }
    }
}
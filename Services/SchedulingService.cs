using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static Google.Apis.Calendar.v3.EventsResource;

namespace Prima.Services
{
    public class SchedulingService
    {
        private ConfigurationService _config;

        private static string[] _scopes = { CalendarService.Scope.CalendarEvents };
        private static string _applicationName = "Prima";

        private CalendarService _service;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public SchedulingService(ConfigurationService config)
        {
            _config = config;

            using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                _scopes,
                "user",
                CancellationToken.None,
                new FileDataStore(_config.GTokenFile, true)
            ).Result;
            Log.Information("Credential file saved to {File}", _config.GTokenFile);

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
    }
}

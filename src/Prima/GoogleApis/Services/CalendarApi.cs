using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prima.GoogleApis.Calendar;
using Serilog;

namespace Prima.GoogleApis.Services
{
    public class CalendarApiException : Exception
    {
        public CalendarApiException(string message) : base(message)
        {
        }
    }

    public class CalendarApi
    {
        private const string BaseAddress = "http://localhost:7552/calendar";
        private const string CannotConnectMessage = "Could not connect to Calendar API service.";

        private readonly HttpClient _http;

        public CalendarApi(HttpClient http) => _http = http;

        public async Task<IEnumerable<MiniEvent>> GetEvents(string calendarClass)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}");
            try
            {
                var res = await _http.GetStringAsync(uri);
                return JsonConvert.DeserializeObject<MiniEvent[]>(res);
            }
            catch (HttpRequestException)
            {
                Log.Warning(CannotConnectMessage);
                return Array.Empty<MiniEvent>();
            }
        }

        public async Task<string> PostEvent(string calendarClass, MiniEvent newEvent)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}");
            using var newEventJson = new StringContent(JsonConvert.SerializeObject(newEvent));
            try
            {
                var res = await _http.PostAsync(uri, newEventJson);
                var body = await res.Content.ReadAsStringAsync();
                var ecr = JsonConvert.DeserializeObject<EventCreateResponse>(body);
                return ecr?.EventLink ?? throw new CalendarApiException("Failed to deserialize API response.");
            }
            catch (HttpRequestException)
            {
                Log.Warning(CannotConnectMessage);
                return null;
            }
        }

        public async Task<bool> UpdateEvent(string calendarClass, MiniEvent newEvent)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}/{newEvent.ID}");
            using var newEventJson = new StringContent(JsonConvert.SerializeObject(newEvent));
            try
            {
                await _http.PutAsync(uri, newEventJson);
                return true;
            }
            catch (HttpRequestException)
            {
                Log.Warning(CannotConnectMessage);
                return false;
            }
        }

        public async Task<MiniEvent> GetEvent(string calendarClass, string id)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}/{id}");
            try
            {
                var res = await _http.GetStringAsync(uri);
                var me = JsonConvert.DeserializeObject<MiniEvent>(res);
                return me;
            }
            catch (HttpRequestException)
            {
                Log.Warning(CannotConnectMessage);
                return null;
            }
        }

        public async Task<bool> DeleteEvent(string calendarClass, string id)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}/{id}");
            try
            {
                var res = await _http.DeleteAsync(uri);
                return res.IsSuccessStatusCode;
            }
            catch (HttpRequestException)
            {
                Log.Warning(CannotConnectMessage);
                return false;
            }
        }
    }
}
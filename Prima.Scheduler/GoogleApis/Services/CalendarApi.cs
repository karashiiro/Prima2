using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prima.Scheduler.GoogleApis.Calendar;
using Serilog;

namespace Prima.Scheduler.GoogleApis.Services
{
    public class CalendarApi
    {
        private const string BaseAddress = "http://localhost:7552/calendar";

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
                Log.Warning("Could not connect to API server.");
                return new MiniEvent[0];
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
                return ecr.EventLink;
            }
            catch (HttpRequestException)
            {
                Log.Warning("Could not connect to API server.");
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
                Log.Warning("Could not connect to API server.");
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
                Log.Warning("Could not connect to API server.");
                return null;
            }
        }

        public async Task<bool> DeleteEvent(string calendarClass, string id)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}/{id}");
            try
            {
                var res = await _http.DeleteAsync(uri);
                var body = await res.Content.ReadAsStringAsync();
                var gr = JsonConvert.DeserializeObject<GenericResponse>(body);
                return true;
            }
            catch (HttpRequestException)
            {
                Log.Warning("Could not connect to API server.");
                return false;
            }
        }
    }
}
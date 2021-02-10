using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prima.Scheduler.GoogleApis;
using Prima.Scheduler.GoogleApis.Calendar;
using Serilog;

namespace Prima.Scheduler.Services
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

        public async Task<EventCreateResponse> PostEvent(string calendarClass, MiniEvent newEvent)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}");
            using var newEventJson = new StringContent(JsonConvert.SerializeObject(newEvent));
            try
            {
                var res = await _http.PostAsync(uri, newEventJson);
                var body = await res.Content.ReadAsStringAsync();
                var ecr = JsonConvert.DeserializeObject<EventCreateResponse>(body);
                return ecr;
            }
            catch (HttpRequestException)
            {
                Log.Warning("Could not connect to API server.");
                return null;
            }
        }

        public async Task<GenericResponse> DeleteEvent(string calendarClass, EventDeleteRequest edr)
        {
            var uri = new Uri($"{BaseAddress}/{calendarClass}");
            using var deleteReqJson = new StringContent(JsonConvert.SerializeObject(edr));
            try
            {
                var res = await _http.PostAsync(uri, deleteReqJson);
                var body = await res.Content.ReadAsStringAsync();
                var gr = JsonConvert.DeserializeObject<GenericResponse>(body);
                return gr;
            }
            catch (HttpRequestException)
            {
                Log.Warning("Could not connect to API server.");
                return null;
            }
        }
    }
}
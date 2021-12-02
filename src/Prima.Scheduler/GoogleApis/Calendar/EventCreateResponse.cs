using Newtonsoft.Json;

namespace Prima.Scheduler.GoogleApis.Calendar
{
    public class EventCreateResponse
    {
        [JsonProperty("eventLink")]
        public string EventLink { get; set; }
    }
}
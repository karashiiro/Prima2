using Newtonsoft.Json;

namespace Prima.Scheduler.GoogleApis.Calendar
{
    public class EventDeleteRequest
    {
        [JsonProperty("id")]
        public string ID { get; set; }
    }
}
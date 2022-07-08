using Newtonsoft.Json;

namespace Prima.GoogleApis.Calendar
{
    public class EventCreateResponse
    {
        [JsonProperty("eventLink")]
        public string EventLink { get; set; }
    }
}
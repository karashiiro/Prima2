using Newtonsoft.Json;

namespace Prima.Scheduler.GoogleApis.Calendar
{
    public class MiniEvent
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }
    }
}
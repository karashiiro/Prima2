using Newtonsoft.Json;

namespace Prima.Scheduler.GoogleApis
{
    public class GenericResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
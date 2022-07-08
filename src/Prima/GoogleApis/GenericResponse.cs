using Newtonsoft.Json;

namespace Prima.GoogleApis
{
    public class GenericResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
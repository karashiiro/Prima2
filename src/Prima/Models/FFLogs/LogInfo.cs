using Newtonsoft.Json;

namespace Prima.Models.FFLogs
{
    public class LogInfo
    {
        [JsonProperty("data")]
        public ReportDataWrapper Content { get; set; }

        public class ReportDataWrapper
        {
            [JsonProperty("reportData")]
            public ReportData Data { get; set; }

            public class ReportData
            {
                [JsonProperty("report")]
                public Report ReportInfo { get; set; }

                public class Report
                {
                    [JsonProperty("fights")]
                    public Fight[] Fights { get; set; }

                    [JsonProperty("masterData")]
                    public Master MasterData { get; set; }

                    public class Fight
                    {
                        [JsonProperty("kill")]
                        public bool? Kill { get; set; }

                        [JsonProperty("name")]
                        public string Name { get; set; }

                        [JsonProperty("friendlyPlayers")]
                        public int[] FriendlyPlayers { get; set; }

                        [JsonProperty("difficulty")]
                        public int? Difficulty { get; set; }
                    }

                    public class Master
                    {
                        [JsonProperty("actors")]
                        public Actor[] Actors { get; set; }

                        public class Actor
                        {
                            [JsonProperty("id")]
                            public int Id { get; set; }

                            [JsonProperty("name")]
                            public string Name { get; set; }

                            [JsonProperty("server")]
                            public string Server { get; set; }
                        }
                    }
                }
            }
        }
    }
}
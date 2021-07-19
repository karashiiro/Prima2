using Newtonsoft.Json;

namespace Prima.Game.FFXIV.XIVAPI
{
    public class Item
    {
        [JsonProperty("ID")]
        public ushort Id { get; set; }

        public string Icon { get; set; }

        public string Name { get; set; }

        public string Url { get; set; }

        public string UrlType { get; set; }
    }
}

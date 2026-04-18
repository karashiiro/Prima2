using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Prima.Game.FFXIV
{
    /// <summary>
    /// A simple character data object returned by the Lodestone Lambda.
    /// </summary>
    public class LodestoneCharacter
    {
        [JsonProperty("bio")]
        public string Bio { get; set; } = "";

        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("world")]
        public string World { get; set; } = "";

        [JsonProperty("avatar")]
        public string Avatar { get; set; } = "";
    }

    /// <summary>
    /// Client for the Lodestone Lambda API.
    /// </summary>
    public class LodestoneClient : IDisposable
    {
        private readonly HttpClient _client;

        public LodestoneClient(string baseUrl)
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/')),
            };
        }

        /// <summary>
        /// Fetches a character by Lodestone ID.
        /// </summary>
        public async Task<LodestoneCharacter?> GetCharacter(string id)
        {
            var body = JsonConvert.SerializeObject(new { id });
            var response = await _client.PostAsync("/character",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<LodestoneCharacter>(json);
        }

        /// <summary>
        /// Searches for a character by name and world. Returns the Lodestone ID if found.
        /// </summary>
        public async Task<ulong?> SearchCharacter(string name, string world)
        {
            var nameParts = name.Split(' ', 2);
            if (nameParts.Length < 2)
                return null;

            var body = JsonConvert.SerializeObject(new
            {
                world,
                firstName = nameParts[0],
                lastName = nameParts[1],
            });

            var response = await _client.PostAsync("/character/search",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<SearchResult>(json);
            return result?.Id;
        }

        /// <summary>
        /// Not yet implemented via the Lodestone Lambda.
        /// </summary>
        public Task GetCharacterAchievement(string id, int page)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        private class SearchResult
        {
            [JsonProperty("id")]
            public ulong Id { get; set; }
        }
    }
}

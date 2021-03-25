using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prima.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Stable.Services
{
    [Serializable]
    public class CharacterNotFound : Exception
    {
        public CharacterNotFound() { }
        public CharacterNotFound(string message) : base(message) { }
        public CharacterNotFound(string message, Exception inner) : base(message, inner) { }
        protected CharacterNotFound(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class NotMatchingFilter : Exception
    {
        public NotMatchingFilter() { }
        public NotMatchingFilter(string message) : base(message) { }
        public NotMatchingFilter(string message, Exception inner) : base(message, inner) { }
        protected NotMatchingFilter(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class CharacterLookup
    {
        private const string ServiceLocation = "http://localhost:7652";

        private readonly HttpClient _http;

        public CharacterLookup(HttpClient http)
        {
            _http = http;
        }

        public async Task<GodestoneCharacterSearchResult> SearchCharacter(string world, string name)
        {
            var response = await _http.GetStringAsync(ServiceLocation + $"/character/search/{world}/{name}");
            var results = JsonConvert.DeserializeObject<GodestoneCharacterSearchResult[]>(response);
            return results.FirstOrDefault(c =>
                string.Equals(c.World, world, StringComparison.InvariantCultureIgnoreCase) &&
                string.Equals(c.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task<JObject> GetCharacter(ulong id)
        {
            var response = await _http.GetAsync(ServiceLocation + $"/character/{id}");
            if (!response.IsSuccessStatusCode)
            {
                throw new CharacterNotFound();
            }

            return JObject.Parse(await response.Content.ReadAsStringAsync());
        }

        public async Task<AchievementInfo[]> GetCharacterAchievements(ulong id)
        {
            var response = await _http.GetAsync(ServiceLocation + $"/character/{id}/achievements");
            if (!response.IsSuccessStatusCode)
            {
                throw new CharacterNotFound();
            }

            return JsonConvert.DeserializeObject<AchievementInfo[]>(await response.Content.ReadAsStringAsync());
        }

        public async Task<MountInfo[]> GetCharacterMounts(ulong id)
        {
            var response = await _http.GetAsync(ServiceLocation + $"/character/{id}/mounts");
            if (!response.IsSuccessStatusCode)
            {
                throw new CharacterNotFound();
            }

            return JsonConvert.DeserializeObject<MountInfo[]>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// Searches for a character meeting a minimum combat job level, and returns a <see cref="DiscordXIVUser"/> if possible.
        /// </summary>
        /// <exception cref="CharacterNotFound"></exception>
        /// <exception cref="NotMatchingFilter"></exception>
        public async Task<DiscordXIVUser> GetDiscordXIVUser(string world, string name, int minimumJobLevel)
        {
            var character = await SearchCharacter(world, name);
            if (character == null)
            {
                throw new CharacterNotFound();
            }

            return await GetDiscordXIVUser(character.ID, minimumJobLevel);
        }

        /// <summary>
        /// Searches for a character meeting a minimum combat job level, and returns a <see cref="DiscordXIVUser"/> if possible.
        /// </summary>
        /// <exception cref="CharacterNotFound"></exception>
        /// <exception cref="NotMatchingFilter"></exception>
        public async Task<DiscordXIVUser> GetDiscordXIVUser(ulong id, int minimumJobLevel)
        {
            var data = await GetCharacter(id);

            // Make sure one of their job levels meet some value.
            var meetsLevel = false;
            foreach (var classJob in data["ClassJobs"].ToObject<ClassJob[]>())
            {
                if (classJob.JobID < 8 || classJob.JobID > 18)
                {
                    if (classJob.Level >= minimumJobLevel)
                    {
                        meetsLevel = true;
                    }
                }
            }
            if (!meetsLevel)
            {
                throw new NotMatchingFilter();
            }

            return new DiscordXIVUser
            {
                DiscordId = 0,
                LodestoneId = id.ToString(),
                Avatar = data["Avatar"].ToObject<string>(),
                Name = data["Name"].ToObject<string>(),
                World = data["World"].ToObject<string>(),
            };
        }

        public class ClassJob
        {
            public int ClassID { get; set; }
            public long ExpLevel { get; set; }
            public long ExpLevelMax { get; set; }
            public long ExpLevelTogo { get; set; }
            public bool IsSpecialized { get; set; }
            public int JobID { get; set; }
            public int Level { get; set; }
            public string Name { get; set; }
        }
    }

    public class GodestoneCharacterSearchResult
    {
        public string Avatar { get; set; }
        public uint ID { get; set; }
        public string Lang { get; set; }
        public string Name { get; set; }
        public int Rank { get; set; }
        public string RankIcon { get; set; }
        public string World { get; set; }
        public string DC { get; set; }
    }

    public class AchievementInfo
    {
        public string Name { get; set; }
        public uint ID { get; set; }
        public string Date { get; set; }
    }

    public class MountInfo
    {
        public string Name { get; set; }
        public uint ID { get; set; }
    }
}
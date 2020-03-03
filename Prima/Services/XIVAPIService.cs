using Newtonsoft.Json.Linq;
using Prima.Models;
using Prima.XIVAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Services
{
    [Serializable]
    public class XIVAPICharacterNotFoundException : Exception
    {
        public XIVAPICharacterNotFoundException() { }
        public XIVAPICharacterNotFoundException(string message) : base(message) { }
        public XIVAPICharacterNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected XIVAPICharacterNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class XIVAPINotMatchingFilterException : Exception
    {
        public XIVAPINotMatchingFilterException() { }
        public XIVAPINotMatchingFilterException(string message) : base(message) { }
        public XIVAPINotMatchingFilterException(string message, Exception inner) : base(message, inner) { }
        protected XIVAPINotMatchingFilterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class XIVAPIService
    {
        private readonly HttpClient _http;

        private const string BASE_URL = "https://xivapi.com";

        public XIVAPIService(HttpClient http) => _http = http;

        /// <summary>
        /// Gets a character from the Lodestone by their Lodestone ID.
        /// </summary>
        public async Task<Character> GetCharacter(ulong id)
        {
            HttpResponseMessage xivapiResponse = await _http.GetAsync(new Uri($"{BASE_URL}/character/{id}?data=CJ,AC,MiMo"));
            string dataObject = await xivapiResponse.Content.ReadAsStringAsync();
            JObject parsedResponse = JObject.Parse(dataObject);
            return new Character(parsedResponse);
        }

        /// <summary>
        /// Searches the Lodestone for a character.
        /// </summary>
        public async Task<CharacterSearchResult> SearchCharacter(string world, string name)
        {
            HttpResponseMessage xivapiResponse = await _http.GetAsync(new Uri($"{BASE_URL}/character/search?name={name}&server={world}"));
            string dataObject = await xivapiResponse.Content.ReadAsStringAsync();
            JObject parsedResponse = JObject.Parse(dataObject);
            IList<JToken> results = parsedResponse["Results"].Children().ToList();
            foreach (JToken result in results)
            {
                CharacterSearchResult entry = result.ToObject<CharacterSearchResult>();
                if (entry.Name.ToLower() == name.ToLower())
                    return entry;
            }
            return new CharacterSearchResult
            {
                Avatar = "",
                FeastMatches = 0,
                ID = 0,
                Lang = "",
                Name = "",
                Rank = null,
                RankIcon = null,
                Server = "",
            };
        }

        /// <summary>
        /// Searches for a character meeting a minimum combat job level, and returns a <see cref="DiscordXIVUser"/> if possible.
        /// </summary>
        /// <exception cref="XIVAPICharacterNotFoundException"></exception>
        /// <exception cref="XIVAPINotMatchingFilterException"></exception>
        public async Task<DiscordXIVUser> GetDiscordXIVUser(string world, string name, int minimumJobLevel)
        {
            // Fetch the character.
            CharacterSearchResult result = await SearchCharacter(world, name);
            if (result.Name.Length == 0)
            {
                throw new XIVAPICharacterNotFoundException();
            }
            Character character = await GetCharacter(result.ID);

            // Make sure one of their job levels meet some value.
            bool meetsLevel = false;
            foreach (ClassJob classJob in character.GetClassJobs())
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
                throw new XIVAPINotMatchingFilterException();
            }

            return new DiscordXIVUser
            {
                DiscordId = 0,
                LodestoneId = result.ID,
                Avatar = result.Avatar,
                Name = result.Name,
                World = world,
            };
        }
    }
}
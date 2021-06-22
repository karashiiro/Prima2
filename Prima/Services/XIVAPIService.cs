using Newtonsoft.Json.Linq;
using Prima.Models;
using Prima.XIVAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.DiscordNet.Services
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
        /// Search all of XIVAPI for a piece of data.
        /// </summary>
        public async Task<IList<T>> Search<T>(string contentName)
        {
            var xivapiResponse = await _http.GetAsync(new Uri($"{BASE_URL}/search?string={contentName}"));
            var dataObject = await xivapiResponse.Content.ReadAsStringAsync();
            return JObject.Parse(dataObject)["Results"].ToObject<IList<T>>();
        }

        /// <summary>
        /// Gets a character from the Lodestone by their Lodestone ID.
        /// </summary>
        public async Task<Character> GetCharacter(ulong id)
        {
            var xivapiResponse = await _http.GetAsync(new Uri($"{BASE_URL}/character/{id}?data=CJ,AC,MiMo"));
            var parsedResponse = await ParseHttpContent(xivapiResponse.Content);
            return new Character(parsedResponse);
        }

        /// <summary>
        /// Searches the Lodestone for a character.
        /// </summary>
        public async Task<CharacterSearchResult> SearchCharacter(string world, string name, string defaultDataCenter = "")
        {
            var xivapiResponse = await _http.GetAsync(new Uri($"{BASE_URL}/character/search?name={name}&server={world}"));
            var parsedResponse = await ParseHttpContent(xivapiResponse.Content);
            if (!parsedResponse["Results"].Children().Any())
            {
                xivapiResponse = await _http.GetAsync(new Uri($"{BASE_URL}/character/search?name={name}&server={world}"));
                parsedResponse = await ParseHttpContent(xivapiResponse.Content);
            }

            IList<JToken> results = parsedResponse["Results"].Children().ToList();
            foreach (var result in results)
            {
                var entry = result.ToObject<CharacterSearchResult>();
                if (!string.IsNullOrEmpty(world))
                {
                    if (entry.Name.ToLower() == name.ToLower())
                        return entry;
                }
                else
                {
                    var dcName = entry.Server.Split(' ')[1];
                    dcName = dcName[1..^1];
                    if (entry.Name.ToLower() == name.ToLower() && dcName == defaultDataCenter)
                        return entry;
                }
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
            var result = await SearchCharacter(world, name);
            if (result.Name.Length == 0)
            {
                throw new XIVAPICharacterNotFoundException();
            }
            var character = await GetCharacter(result.ID);

            // Make sure one of their job levels meet some value.
            var meetsLevel = false;
            foreach (var classJob in character.GetClassJobs())
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
                LodestoneId = result.ID.ToString(),
                Avatar = result.Avatar,
                Name = result.Name,
                World = world,
            };
        }

        /// <summary>
        /// Searches for a character meeting a minimum combat job level, and returns a <see cref="DiscordXIVUser"/> if possible.
        /// </summary>
        /// <exception cref="XIVAPICharacterNotFoundException"></exception>
        /// <exception cref="XIVAPINotMatchingFilterException"></exception>
        public async Task<DiscordXIVUser> GetDiscordXIVUser(ulong lodestoneId, int minimumJobLevel)
        {
            var character = await GetCharacter(lodestoneId);

            // Make sure one of their job levels meet some value.
            var meetsLevel = false;
            foreach (var classJob in character.GetClassJobs())
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
                LodestoneId = lodestoneId.ToString(),
                Avatar = character.XivapiResponse["Character"]["Avatar"].ToObject<string>(),
                Name = character.XivapiResponse["Character"]["Name"].ToObject<string>(),
                World = character.XivapiResponse["Character"]["Server"].ToObject<string>(),
            };
        }

        private async Task<JObject> ParseHttpContent(HttpContent content)
        {
            var data = await content.ReadAsStringAsync();
            return JObject.Parse(data);
        }
    }
}
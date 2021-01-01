using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Prima.Models;
using Prima.Services;
using Serilog;

namespace Prima.Stable.Services
{
    public class LodestoneCharacterService
    {
        [Serializable]
        public class CharacterNotFoundException : Exception
        {
            public CharacterNotFoundException() { }
            public CharacterNotFoundException(string message) : base(message) { }
            public CharacterNotFoundException(string message, Exception inner) : base(message, inner) { }
            protected CharacterNotFoundException(
                System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public class NotMatchingFilterException : Exception
        {
            public NotMatchingFilterException() { }
            public NotMatchingFilterException(string message) : base(message) { }
            public NotMatchingFilterException(string message, Exception inner) : base(message, inner) { }
            protected NotMatchingFilterException(
                System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        private const string ServiceLocation = "http://localhost:5059";

        private readonly HttpClient _http;

        public LodestoneCharacterService(HttpClient http)
        {
            _http = http;
        }

        public async Task<DiscordXIVUser> GetDiscordXIVUser(string world, string name, int minimumJobLevel)
        {
            var characterStr = await Get(name, world);
            if (characterStr == null) throw new CharacterNotFoundException();

            var character = JObject.Parse(characterStr);

            // Make sure one of their job levels meet some value.
            var meetsLevel = false;
            foreach (var classJob in character["ClassJobs"].ToObject<IList<ClassJob>>())
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
                throw new NotMatchingFilterException();
            }

            return new DiscordXIVUser
            {
                DiscordId = 0,
                LodestoneId = character["ID"].ToObject<uint>().ToString(),
                Avatar = character["Avatar"].ToObject<string>(),
                Name = character["Name"].ToObject<string>(),
                World = world,
            };
        }

        /// <exception cref="XIVAPINotMatchingFilterException"></exception>
        public async Task<DiscordXIVUser> GetDiscordXIVUser(ulong lodestoneId, int minimumJobLevel)
        {
            var characterStr = await Get(lodestoneId.ToString());
            if (characterStr == null) throw new CharacterNotFoundException();

            var character = JObject.Parse(characterStr);

            // Make sure one of their job levels meet some value.
            var meetsLevel = false;
            foreach (var classJob in character["ClassJobs"].ToObject<IList<ClassJob>>())
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
                throw new NotMatchingFilterException();
            }

            return new DiscordXIVUser
            {
                DiscordId = 0,
                LodestoneId = lodestoneId.ToString(),
                Avatar = character["Avatar"].ToObject<string>(),
                Name = character["Name"].ToObject<string>(),
                World = character["World"].ToObject<string>(),
            };
        }

        private async Task<string> Get(params string[] args)
        {
            using var req = new StringContent(string.Join(" ", args));
            try
            {
                var res = await _http.PostAsync(new Uri(ServiceLocation), req);
                return await res.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Log.Error(e, "Character fetch service offline.");
                return null;
            }
        }

        private class ClassJob
        {
            public byte ClassID { get; set; }
            public uint ExpLevel { get; set; }
            public uint ExpLevelMax { get; set; }
            public uint ExpLevelTogo { get; set; }
            public bool IsSpecialized { get; set; }
            public byte JobID { get; set; }
            public byte Level { get; set; }
            public string Name { get; set; }
            public UnlockedState UnlockedState { get; set; }
        }

        private class UnlockedState
        {
            public byte ID { get; set; }
            public string Name { get; set; }
        }
    }
}
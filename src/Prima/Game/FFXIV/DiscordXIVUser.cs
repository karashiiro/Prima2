using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NetStone;
using NetStone.Model.Parseables.Character;
using NetStone.Search.Character;

namespace Prima.Game.FFXIV
{
    public class DiscordXIVUser
    {
        [BsonId]
        [BsonRequired]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "<Pending>")]
#pragma warning disable 169
        private ObjectId _id;
#pragma warning restore 169

        [BsonRequired] [BsonRepresentation(BsonType.String)]
        public ulong DiscordId;

        [BsonRequired] [BsonRepresentation(BsonType.String)]
        public string LodestoneId;

        [BsonRequired] public string World;

        [BsonRequired] public string Name;

        [BsonRequired] public string Avatar;

        public string Timezone;

        public bool Verified;

        // Keyed on guild ID (keys must be represented as strings)
        public Dictionary<string, IList<ulong>> VanityRoles = new();

        public IList<ulong> GetVanityRoles(ulong guildId)
        {
            return VanityRoles?.GetValueOrDefault(guildId.ToString(), new List<ulong>()) ?? new List<ulong>();
        }

        public void AddVanityRoles(ulong guildId, IEnumerable<ulong> roles)
        {
            VanityRoles ??= new Dictionary<string, IList<ulong>>();
            if (!VanityRoles.ContainsKey(guildId.ToString())) VanityRoles.Add(guildId.ToString(), new List<ulong>());

            foreach (var role in roles)
            {
                VanityRoles[guildId.ToString()].Add(role);
            }
        }

        public static async Task<(DiscordXIVUser?, LodestoneCharacter?)> CreateFromLodestoneId(
            LodestoneClient lodestone,
            ulong lodestoneId, ulong discordId)
        {
            var character = await lodestone.GetCharacter(lodestoneId.ToString());
            if (character == null)
            {
                return (null, null);
            }

            return (new DiscordXIVUser
            {
                DiscordId = discordId,
                LodestoneId = lodestoneId.ToString(),
                World = character.Server,
                Name = character.Name,
                Avatar = character.Avatar.ToString(),
            }, character);
        }

        public static async Task<(DiscordXIVUser?, LodestoneCharacter?)> CreateFromLodestoneSearch(
            LodestoneClient lodestone,
            string name, string world, ulong discordId)
        {
            int numPages;
            var pageNumber = 1;
            var lodestoneId = "";
            LodestoneCharacter? character = null;
            do
            {
                var page = await lodestone.SearchCharacter(new CharacterSearchQuery
                    { CharacterName = name, World = world }, pageNumber);
                if (page == null) throw new InvalidOperationException("Failed to retrieve search page");
                numPages = page.NumPages;

                foreach (var c in page.Results)
                {
                    if (!string.Equals(c.Name, name, StringComparison.InvariantCultureIgnoreCase)) continue;

                    lodestoneId = c.Id;
                    character = await c.GetCharacter();
                    if (character == null) continue;
                    if (string.Equals(character.Name, name, StringComparison.InvariantCultureIgnoreCase)) break;
                }

                pageNumber++;
            } while (pageNumber != numPages && pageNumber < 10);

            if (character != null)
            {
                return (new DiscordXIVUser
                {
                    DiscordId = discordId,
                    LodestoneId = lodestoneId,
                    World = character.Server,
                    Name = character.Name,
                    Avatar = character.Avatar.ToString(),
                }, character);
            }

            return (null, null);
        }
    }
}
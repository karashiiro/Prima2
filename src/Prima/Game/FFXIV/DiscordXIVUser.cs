using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

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

        [Obsolete("No longer used.")]
        public Dictionary<string, ulong?> PrioritizedVanityRole = new();

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
                if (!VanityRoles[guildId.ToString()].Contains(role)) VanityRoles[guildId.ToString()].Add(role);
            }

            // Temporary hack since some users have duplicates already
            VanityRoles[guildId.ToString()] = new HashSet<ulong>(VanityRoles[guildId.ToString()]).ToList();
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
                World = character.World,
                Name = character.Name,
                Avatar = character.Avatar,
            }, character);
        }

        public static async Task<(DiscordXIVUser?, LodestoneCharacter?)> CreateFromLodestoneSearch(
            LodestoneClient lodestone,
            string name, string world, ulong discordId)
        {
            var id = await lodestone.SearchCharacter(name, world);
            if (id == null)
            {
                return (null, null);
            }

            var character = await lodestone.GetCharacter(id.Value.ToString());
            if (character == null)
            {
                return (null, null);
            }

            return (new DiscordXIVUser
            {
                DiscordId = discordId,
                LodestoneId = id.Value.ToString(),
                World = character.World,
                Name = character.Name,
                Avatar = character.Avatar,
            }, character);
        }
    }
}

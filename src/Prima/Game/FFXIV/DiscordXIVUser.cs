using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using JetBrains.Annotations;
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
                    lodestoneId = c.Id;
                    character = await c.GetCharacter();
                    if (character == null) continue;
                    if (character.Name == name) break;
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
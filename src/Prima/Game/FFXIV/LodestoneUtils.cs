using Serilog;
using System.Threading.Tasks;

namespace Prima.Game.FFXIV
{
    /// <summary>
    /// LodestoneUtils is a collection of Lodestone-related utilities that don't strictly belong
    /// on a particular related service.
    /// </summary>
    public static class LodestoneUtils
    {
        /// <summary>
        /// Verifies a Lodestone character using a token expected to be found in that character's bio.
        /// </summary>
        /// <param name="lookupService">The service used to fetch the character.</param>
        /// <param name="lodestoneId">The Lodestone ID of the character to check.</param>
        /// <param name="token">The token to search for in the character's bio.</param>
        /// <returns></returns>
        public static async Task<bool> VerifyCharacter(CharacterLookup lookupService, ulong lodestoneId, string token)
        {
            var character = await lookupService.GetCharacter(lodestoneId);

            var bio = character["Bio"]?.ToObject<string>();
            if (bio == null)
            {
                Log.Error("Character bio is null (id={LodestoneId})", lodestoneId);
                return false;
            }

            Log.Information("{Bio}, {Token}", bio, token);
            return bio.Contains(token);
        }
    }
}
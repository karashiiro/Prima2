using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Prima.XIVAPI
{
    public class Character
    {
        public JObject XivapiResponse { get; }

        public Character(JObject xivapiResponse) => XivapiResponse = xivapiResponse;

        /// <summary>
        /// Get the <see cref="Character"/>'s achievements.
        /// </summary>
        public IEnumerable<AchievementListEntry> GetAchievements()
        {
            IList<JToken> results = XivapiResponse["Achievements"]["List"].Children().ToList();
            return results.Select(result => result.ToObject<AchievementListEntry>()).ToList();
        }

        /// <summary>
        /// Get the <see cref="Character"/>'s minions and mounts.
        /// </summary>
        public IEnumerable<MinionMount> GetMiMo()
        {
            var minions = XivapiResponse["Minions"].Children();
            var mounts = XivapiResponse["Mounts"].Children();
            IList<JToken> results = minions.Concat(mounts).ToList();
            return results.Select(result => result.ToObject<MinionMount>()).ToList();
        }

        /// <summary>
        /// Get the <see cref="Character"/>'s bio.
        /// </summary>
        public string GetBio()
        {
            return XivapiResponse["Character"]["Bio"].ToObject<string>();
        }

        /// <summary>
        /// Get the <see cref="Character"/>'s <see cref="ClassJob"/> information.
        /// </summary>
        public IEnumerable<ClassJob> GetClassJobs()
        {
            IList<JToken> results = XivapiResponse["Character"]["ClassJobs"].Children().ToList();
            return results.Select(result => result.ToObject<ClassJob>()).ToList();
        }
    }

    public struct CharacterSearchResult
    {
        public string Avatar;
        public ushort FeastMatches;
        public ulong ID;
        public string Lang;
        public string Name;
        public object Rank;
        public object RankIcon;
        public string Server;
    }

    public struct ClassJob
    {
        public byte ClassID;
        public ulong ExpLevel;
        public ulong ExpLevelMax;
        public ulong ExpLevelToGo;
        public bool IsSpecialized;
        public byte JobID;
        public byte Level;
        public string Name;
    }
}
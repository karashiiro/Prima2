using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Prima.XIVAPI
{
    public class Character
    {
        private readonly JObject _xivapiResponse;

        public Character(JObject xivapiResponse) => _xivapiResponse = xivapiResponse;

        /// <summary>
        /// Get the <see cref="Character"/>'s achievements.
        /// </summary>
        public IList<AchievementListEntry> GetAchievements()
        {
            IList<JToken> results = _xivapiResponse["Achievements"]["List"].Children().ToList();
            IList<AchievementListEntry> achievements = new List<AchievementListEntry>();
            foreach (JToken result in results)
            {
                AchievementListEntry entry = result.ToObject<AchievementListEntry>();
                achievements.Add(entry);
            }
            return achievements;
        }

        /// <summary>
        /// Get the <see cref="Character"/>'s minions and mounts.
        /// </summary>
        public IList<MinionMount> GetMiMo()
        {
            JEnumerable<JToken> minions = _xivapiResponse["Minions"].Children();
            JEnumerable<JToken> mounts = _xivapiResponse["Mounts"].Children();
            IList<JToken> results = minions.Concat(mounts).ToList();
            IList<MinionMount> mimo = new List<MinionMount>();
            foreach (JToken result in results)
            {
                MinionMount entry = result.ToObject<MinionMount>();
                mimo.Add(entry);
            }
            return mimo;
        }

        /// <summary>
        /// Get the <see cref="Character"/>'s bio.
        /// </summary>
        public string GetBio()
        {
            return _xivapiResponse["Character"]["Bio"].ToObject<string>();
        }

        /// <summary>
        /// Get the <see cref="Character"/>'s <see cref="ClassJob"/> information.
        /// </summary>
        public IList<ClassJob> GetClassJobs()
        {
            IList<JToken> results = _xivapiResponse["Character"]["ClassJobs"].Children().ToList();
            IList<ClassJob> classJobs = new List<ClassJob>();
            foreach (JToken result in results)
            {
                ClassJob entry = result.ToObject<ClassJob>();
                classJobs.Add(entry);
            }
            return classJobs;
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
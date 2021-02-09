using System.Collections.Generic;
using System.Linq;

namespace Prima.Resources
{
    public static class DelubrumProgressionRoles
    {
#if DEBUG
        public const ulong Executor = 807879650893889536; // Delubrum Roler
#else
        public const ulong Executor = 807860249775702066;
#endif

        public static readonly IDictionary<ulong, string> Roles = new Dictionary<ulong, string>
        {
            {807854117053136896, "Trinity Seeker Progression"},
            {807854112905363456, "Dahu Progression"},
            {807854109608378368, "Queen's Guard Progression"},
            {807854104134680617, "Phantom Progression"},
            {807854100204879902, "Trinity Avowed Progression"},
            {807854094286979082, "Stygimoloch Lord Progression"},
            {807854087092830259, "The Queen Progression"},
#if DEBUG
            {807881434110754866, "debug delub role"},
#endif
        };

        public static IList<ulong> GetContingentRoles(ulong roleId)
        {
            var baseList = new ulong[] { 807854087092830259, 807854094286979082, 807854100204879902, 807854104134680617, 807854109608378368, 807854112905363456, 807854117053136896 };
            return roleId switch
            {
                807854087092830259 => baseList,
                807854094286979082 => baseList[1..],
                807854100204879902 => baseList[2..],
                807854104134680617 => baseList[3..],
                807854109608378368 => baseList[4..],
                807854112905363456 => baseList[5..],
                807854117053136896 => baseList[6..],
                _ => new List<ulong>(),
            };
        }
    }
}
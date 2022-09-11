using System;
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

        public const ulong ClearedDelubrumSavage = 806362589134454805;

        public static readonly IDictionary<ulong, string> Roles = new Dictionary<ulong, string>
        {
#if DEBUG
            //section to add dev server roles IDs as needed
            {1017563010367246407, "Trinity Seeker Progression"},
            {1017563202378285186, "Queen's Guard Progression"},
            {1017563257340428328, "Trinity Avowed Progression"},
            {1017563291435941888, "The Queen Progression"},
#else
            {807854117053136896, "Trinity Seeker Progression"},
            {807854109608378368, "Queen's Guard Progression"},
            {807854100204879902, "Trinity Avowed Progression"},
            {807854087092830259, "The Queen Progression"},
#endif
#if DEBUG
            {807881434110754866, "debug delub role"},
#endif
        };

        public static readonly IDictionary<ulong, string> LFGRoles = new Dictionary<ulong, string>
        {
            {810201516291653643, "Fresh Progression"},
            {810201667814948877, "Trinity Seeker Progression"},
            {810201853333602335, "Queen's Guard Progression"},
            {810201890775629877, "Trinity Avowed Progression"},
            {810201946249232384, "The Queen Progression"},
        };

        public static IEnumerable<ulong> GetContingentRoles(ulong roleId)
        {
#if DEBUG
            var baseList = new ulong[] { 1017563291435941888, 1017563257340428328, 1017563202378285186, 1017563010367246407 };
#else
            var baseList = new ulong[] { 807854087092830259, 807854100204879902, 807854109608378368, 807854117053136896 };
#endif
            return roleId switch
            {
#if DEBUG
                1017563291435941888 => baseList,
                1017563257340428328 => baseList[1..],
                1017563202378285186 => baseList[2..],
                1017563010367246407 => baseList[3..],
#else
                807854087092830259 => baseList,
                807854100204879902 => baseList[1..],
                807854109608378368 => baseList[2..],
                807854117053136896 => baseList[3..],
#endif

#if DEBUG
                807881434110754866 => new List<ulong> { 807881434110754866 },
#endif
                _ => new List<ulong>(),
            };
        }

        public static ulong GetKillRole(string roleName)
        {
            return roleName switch
            {
                "Trinity Seeker Progression" => Roles.First(kvp => kvp.Value == "Queen's Guard Progression").Key,
                "Queen's Guard Progression" => Roles.First(kvp => kvp.Value == "Trinity Avowed Progression").Key,
                "Trinity Avowed Progression" => Roles.First(kvp => kvp.Value == "The Queen Progression").Key,
                "The Queen Progression" => ClearedDelubrumSavage,
                _ => throw new NotImplementedException(),
            };
        }
    }
}
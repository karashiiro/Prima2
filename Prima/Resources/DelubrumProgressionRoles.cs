using System.Collections.Generic;

namespace Prima.Resources
{
    public static class DelubrumProgressionRoles
    {
#if DEBUG
        public const ulong Executor = 807879650893889536; // Delubrum Roler
#else
        public const ulong Executor = 807860249775702066;
#endif

        public static readonly IList<ulong> Ids = new List<ulong>
        {
            807854117053136896, // Trinity Seeker Progression
            807854112905363456, // Dahu Progression
            807854109608378368, // Queen's Guard Progression
            807854104134680617, // Phantom Progression
            807854100204879902, // Trinity Avowed Progression
            807854094286979082, // Stygimoloch Lord Progression
            807854087092830259, // The Queen Progression
        };
    }
}
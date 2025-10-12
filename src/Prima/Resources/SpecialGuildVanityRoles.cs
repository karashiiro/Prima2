using System;
using System.Collections.Generic;

namespace Prima.Resources
{
    public static class SpecialGuildVanityRoles
    {
        public static readonly Dictionary<ulong, ulong[]> Roles = new()
        {
            {
                SpecialGuilds.CrystalExploratoryMissions,
                new ulong[]
                {
                    /* Infamy of Blood */ 1381436271548829797,
                    /* Cleared Forked Tower */ 1381433968431202424,
                    /* Savage Queen */ 806363209040134174,
                    /* Cleared Delubrum (Savage) */ 806362589134454805,
                    /* Arsenal Master */ 583790261650456581,
                    /* Cleared Arsenal */ 552639779930636298,
                }
            },
        };

        public static ulong[] GetRoles(ulong guildId)
        {
            return Roles.TryGetValue(guildId, out var roles) ? roles : Array.Empty<ulong>();
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace Prima.Resources
{
    public static class QueueInfo
    {
        public const double BAQueueTimeout = 4 * Time.Hour;
        public const double CastrumQueueTimeout = 4 * Time.Hour;
        public const double DelubrumQueueTimeout = 4 * Time.Hour;

        // This is all really bad and should be done in config.
        public static readonly IDictionary<ulong, string> LfgChannels = new Dictionary<ulong, string>
        {
            { 550708765490675773, "learning-and-frag-farm" },
            { 550708833412972544, "av-and-ozma-prog" },
            { 550708866497773599, "clears-and-farming" },
            {
#if DEBUG
                766712049316265985
#else
                765994301850779709
#endif
                , "lfg-castrum" },
            { 803636739343908894, "lfg-delubrum-savage" },
            { 809241125373739058, "lfg-drs-fresh-prog" },
            { 806957742056013895, "lfg-delubrum-normal" },
        };

        public static IDictionary<string, List<ulong>> FlipDictionary()
        {
            var res = LfgChannels
                .GroupBy(p => p.Value)
                .ToDictionary(g => g.Key, g => g.Select(pp => pp.Key).ToList());
            return res;
        }
    }
}
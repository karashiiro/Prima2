using System.Text.RegularExpressions;

namespace Prima
{
    public static class QueueUtil
    {
        private static readonly Regex DpsCountRegex = new Regex(@"\d+(?=d)", RegexOptions.Compiled);
        private static readonly Regex HealerCountRegex = new Regex(@"\d+(?=h)", RegexOptions.Compiled);
        private static readonly Regex TankCountRegex = new Regex(@"\d+(?=t)", RegexOptions.Compiled);
        public static (int, int, int) GetDesiredRoleCounts(string input)
        {
            int countd = 0, counth = 0, countt = 0;
            input = input.Replace(" ", "");

            var dpsMatches = DpsCountRegex.Match(input);
            if (dpsMatches.Success)
                int.TryParse(dpsMatches.Captures[0].Value, out countd);

            var healerMatches = HealerCountRegex.Match(input);
            if (healerMatches.Success)
                int.TryParse(healerMatches.Captures[0].Value, out counth);

            var tankMatches = TankCountRegex.Match(input);
            if (tankMatches.Success)
                int.TryParse(tankMatches.Captures[0].Value, out countt);

            return (countd, counth, countt);
        }
    }
}
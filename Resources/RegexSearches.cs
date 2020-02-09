using System.Text.RegularExpressions;

namespace Prima.Resources
{
    public static class RegexSearches
    {
        public static readonly Regex AngleBrackets = new Regex(@"[<>]", RegexOptions.Compiled);
        public static readonly Regex NonAlpha = new Regex(@"[^a-zA-Z]+", RegexOptions.Compiled);
        public static readonly Regex NonNumbers = new Regex(@"[^\d]+", RegexOptions.Compiled);
        public static readonly Regex UnicodeApostrophe = new Regex(@"[’]", RegexOptions.Compiled);
    }
}

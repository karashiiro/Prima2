using System.Text.RegularExpressions;

namespace Prima.Resources
{
    public static class RegexSearches
    {
        public static readonly Regex AngleBrackets = new Regex(@"[<>]", RegexOptions.Compiled);
        public static readonly Regex NonAlpha = new Regex(@"[^a-zA-Z]+", RegexOptions.Compiled);
        public static readonly Regex NonNumbers = new Regex(@"[^\d]+", RegexOptions.Compiled);
        public static readonly Regex UnicodeApostrophe = new Regex(@"[’]", RegexOptions.Compiled);

        public static readonly Regex Time = new Regex(@"\d+:\d+\s?(?:(?:AM)|(?:PM))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex TimeHours = new Regex(@"\d+(?=:)", RegexOptions.Compiled);
        public static readonly Regex TimeMinutes = new Regex(@"\d+(?=(?:AM|PM))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex TimeMeridiem = new Regex(@"[^\s\d:]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex DayOrDate = new Regex(@"(?:\d+\/\d+\/\d+)|(?:\d+\/\d+)|(?:\s[^\s]+day)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
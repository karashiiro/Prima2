using System.Text.RegularExpressions;

namespace Prima.Resources
{
    public static class RegexSearches
    {
        public static readonly Regex AngleBrackets = new(@"[<>]", RegexOptions.Compiled);
        public static readonly Regex NonAlpha = new(@"[^a-zA-Z]+", RegexOptions.Compiled);
        public static readonly Regex NonNumbers = new(@"[^\d]+", RegexOptions.Compiled);
        public static readonly Regex UnicodeApostrophe = new(@"[‘’]", RegexOptions.Compiled);
        public static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        public static readonly Regex Multiplier = new(@"x\d", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex Time = new(@"(\d{1,2}:\d{1,2}\s?(?:(?:a|p))?|\d{1,2}\s?(?:(?:a|p)))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex TimeHours = new(@"\d{1,2}(?=:|a|p)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex TimeMinutes = new(@"(?<=:)\d{1,2}(?=(?:$|a|p))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex TimeMeridiem = new(@"[^\s\d:]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex Date = new(@"(?:\d+\/\d+\/\d+)|(?:\d+\/\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex DayOrDate = new(@"(?:\d+\/\d+\/\d+)|(?:\d+\/\d+)|(?:\s[^\s]+day)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Regex ScheduleOutputFieldNameRegex = new(@"(?<!Social|^)ScheduleOutputChannel", RegexOptions.Compiled);
    }
}
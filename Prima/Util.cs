using Prima.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Prima
{
    public static class Util
    {
        /// <summary>
        /// Calculates an incremental average of <see cref="TimeSpan"/> objects.
        /// </summary>
        /// <param name="lastAverage">The last average.</param>
        /// <param name="lastN">The number of items already in the average.</param>
        /// <param name="newValue">The <see cref="TimeSpan"/> to add to the average.</param>
        public static TimeSpan IncrementAverage(TimeSpan lastAverage, int lastN, TimeSpan newValue)
            => (lastN * lastAverage + newValue) / (lastN + 1);

        /// <summary>
        /// Returns the absolute value of a <see cref="TimeSpan"/>.
        /// </summary>
        public static TimeSpan? Abs(this TimeSpan? ts)
        {
            if (ts == null) return null;
            return ts > TimeSpan.Zero ? ts : -ts;
        }

        /// <summary>
        /// Converts a function that requires two arguments into a function that requires one argument.
        /// </summary>
        public static Func<T2, TResult> Apply<T1, T2, TResult>(Func<T1, T2, TResult> f, T1 arg)
            => (x) => f(arg, x);

        /// <summary>
        /// Converts a function that requires three arguments into a function that requires two arguments.
        /// </summary>
        public static Func<T2, T3, TResult> Apply<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> f, T1 arg)
            => (x, y) => f(arg, x, y);

        /// <summary>
        /// Gets the absolute path of a file from its path relative to the executing assembly.
        /// </summary>
        public static string GetAbsolutePath(string relativePath)
            => Path.Combine(Assembly.GetEntryAssembly().Location, "..", relativePath);

        /// <summary>
        /// Gets a day of the week and a time from a string.
        /// </summary>
        public static DateTime GetDateTime(string text)
        {
            DateTime date = DateTime.MinValue;
            Match dayMatch = RegexSearches.DayOrDate.Match(text);
            if (dayMatch.Success)
            {
                string dayOrDate = dayMatch.Value.Trim();
                if (dayOrDate.IndexOf("/") != -1)
                {
                    int[] mmddyyyy = dayOrDate.Split("/").Select(term => int.Parse(term)).ToArray();
                    date = new DateTime(mmddyyyy.Length == 3 ? mmddyyyy[2] : DateTime.Now.Year, mmddyyyy[0], mmddyyyy[1]);
                }
                else
                {
                    var requestedDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOrDate);
                    // https://stackoverflow.com/a/6346190 big fan
                    int daysUntil = (requestedDay - DateTime.Now.DayOfWeek + 7) % 7;
                    date = DateTime.Now.AddDays(daysUntil);
                }
            }
            if (date == DateTime.MinValue)
            {
                throw new ArgumentException();
            }
            Match timeMatch = RegexSearches.Time.Match(text);
            date.AddSeconds(-date.Second)
                   .AddMilliseconds(-date.Millisecond);
            if (timeMatch.Success)
            {
                string time = timeMatch.Value.Replace(" ", "").Trim().ToUpper();
                int hours = int.Parse(RegexSearches.TimeHours.Match(time).Value);
                int minutes = int.Parse(RegexSearches.TimeMinutes.Match(time).Value);
                string meridiem = RegexSearches.TimeMeridiem.Match(time).Value;
                if (meridiem == "PM")
                {
                    hours += 12;
                    hours += 12;
                }
                date.AddHours(hours - date.Hour)
                       .AddMinutes(minutes - date.Minute);
            }
            else
            {
                date.AddHours(-date.Hour)
                       .AddMinutes(-date.Minute);
            }

            return date;
        }
    }
}

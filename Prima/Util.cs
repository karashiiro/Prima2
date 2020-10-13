using Prima.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Prima.Models;
using Serilog;

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
        /// Gets the absolute path of a file from its path relative to the entry assembly.
        /// </summary>
        public static string GetAbsolutePath(string relativePath)
            => Path.Combine(Assembly.GetEntryAssembly().Location, "..", relativePath);

        /// <summary>
        /// Gets a day of the week and a time from a set of strings.
        /// </summary>
        public static DateTime GetDateTime(string keywords)
        {
            // All this is copied from Roo's scheduler (with minor tweaks)
            var year = DateTime.Now.Year;
            var month = DateTime.Now.Month;
            var day = DateTime.Now.Day;
            var hour = DateTime.Now.Hour;
            var minute = DateTime.Now.Minute;
            var dayOfWeek = -1;

            //Check to see if it matches a recognized time format
            var timeResult = RegexSearches.Time.Match(string.Join(' ', keywords));
            if (timeResult.Success)
            {
                var time = timeResult.Value.ToLowerInvariant().Replace(" ", "");
                hour = int.Parse(RegexSearches.TimeHours.Match(time).Value);
                minute = int.Parse(RegexSearches.TimeMinutes.Match(time).Value);
                var meridiem = RegexSearches.TimeMeridiem.Match(time).Value;
                if (!meridiem.StartsWith("a") && hour != 12)
                {
                    hour += 12;
                }

                if (meridiem.StartsWith("a") && hour == 12)
                {
                    hour = 0;
                }
            }

            var splitKeywords = RegexSearches.Whitespace.Split(keywords);

            //Check to see if it matches a recognized date format.
            foreach (var keyword in splitKeywords)
            {
                var dateResult = RegexSearches.Date.Match(keyword);
                if (dateResult.Success)
                {
                    var date = dateResult.Value.Trim();
                    var mmddyyyy = date.Split("/").Select(int.Parse).ToArray();
                    month = mmddyyyy[0];
                    day = mmddyyyy[1];
                    if (mmddyyyy.Length == 3)
                    {
                        year = mmddyyyy[2];
                    }
                    continue;
                }
 
                //Check for days of the week, possibly abbreviated.
                if (dayOfWeek == -1)
                    switch (keyword.ToLowerInvariant())
                    {
                        //Days of the week.
                        case "日":
                        case "日曜日":
                        case "su":
                        case "sun":
                        case "sunday":
                            dayOfWeek = (int)DayOfWeek.Sunday;
                            continue;
                        case "月":
                        case "月曜日":
                        case "m":
                        case "mo":
                        case "mon":
                        case "monday":
                            dayOfWeek = (int)DayOfWeek.Monday;
                            continue;
                        case "火":
                        case "火曜日":
                        case "t":
                        case "tu":
                        case "tue":
                        case "tues":
                        case "tuesday":
                            dayOfWeek = (int)DayOfWeek.Tuesday;
                            continue;
                        case "水":
                        case "水曜日":
                        case "w":
                        case "wed":
                        case "wednesday":
                            dayOfWeek = (int)DayOfWeek.Wednesday;
                            continue;
                        case "木":
                        case "木曜日":
                        case "th":
                        case "thu":
                        case "thursday":
                            dayOfWeek = (int)DayOfWeek.Thursday;
                            continue;
                        case "金":
                        case "金曜日":
                        case "f":
                        case "fri":
                        case "friday":
                            dayOfWeek = (int)DayOfWeek.Friday;
                            continue;
                        case "土":
                        case "土曜日":
                        case "sa":
                        case "sat":
                        case "saturday":
                            dayOfWeek = (int)DayOfWeek.Saturday;
                            continue;
                    }
            } //foreach
            
            //Check to make sure everything got set here, and then...
            var finalDate = new DateTime(year, month, day, hour, minute, 0);
            if (dayOfWeek >= 0)
            {
                finalDate = finalDate.AddDays((dayOfWeek - (int)finalDate.DayOfWeek + 7) % 7);
            }
            
            return finalDate;
        }

        public static string GetClosestString(string input, IEnumerable<string> options)
        {
            var output = options.First();
            foreach (var option in options)
                if (Levenshtein(input, output) > Levenshtein(input, option))
                    output = option;
            return output;
        }

        /// <summary>
        /// Calculate the Levenshtein distance between two strings.
        /// </summary>
        public static int Levenshtein(string string1, string string2)
        {
            var a = string1.Length;
            var b = string2.Length;
            if (a == 0)
                return b;
            if (b == 0)
                return a;
            var costBonus = Math.Abs(a - b); // If there's a difference between the strings, that means an insert for every extra character.
            a = Math.Min(a, b); // These then can be equal since we've pulled out the difference already.
            b = a;
            var matrix = new int[a, b];
            for (var k = 0; k < a; k++)
            {
                matrix[0, k] = k;
                matrix[k, 0] = k;
            }
            for (var i = 1; i < a; i++)
            {
                for (var j = 1; j < b; j++)
                {
                    if (string1[i] == string2[j])
                        matrix[i, j] = Math.Min(matrix[i - 1, j - 1], matrix[i, j - 1]);
                    else
                        matrix[i, j] = Math.Min(matrix[i - 1, j - 1], Math.Min(matrix[i, j - 1], matrix[i - 1, j])) + 1;
                }
            }
            return matrix[a - 1,b - 1];
        }

        public static (TimeZoneInfo, DateTime) GetLocalizedTimeForUser(DiscordXIVUser user, DateTime time)
        {
            TimeZoneInfo tzi = null;
            DateTime outTime = default;

            // ReSharper disable once InvertIf
            if (user != null)
            {
                try
                {
                    tzi = TimeZoneInfo.FindSystemTimeZoneById(user.Timezone);
                    outTime = TimeZoneInfo.ConvertTime(time, time.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local, tzi);
                }
                catch (ArgumentNullException) { }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            return (tzi, outTime);
        }

        public static string Capitalize(string input)
            => char.ToUpperInvariant(input[0]) + input.Substring(1).ToLowerInvariant();

        public static string JadenCase(string input)
            => input.Split(" ").Select(Capitalize).Aggregate((workingSentence, nextWord) => workingSentence + " " + nextWord);

        public static string ToAbbreviation(string input)
            => input.Split(" ").Select(word => char.ToUpperInvariant(word[0]).ToString()).Aggregate((str, c) => str + c);

        /// <summary>
        /// Get the value of an object property by its string name.
        /// </summary>
        public static object GetPropertyValue(this object? obj, string propName)
            => obj.GetType().GetProperty(propName).GetValue(obj, null);

        /// <summary>
        /// Returns true if the object has the specified property.
        /// </summary>
        public static bool HasProperty(this object? obj, string propName)
            => obj.GetType().GetProperties().Where(pi => pi.Name == propName).Any();

        /// <summary>
        /// Returns true if the object has the specified field.
        /// </summary>
        public static bool HasField(this object? obj, string fieldName)
            => obj.GetType().GetFields().Where(pi => pi.Name == fieldName).Any();

        /// <summary>
        /// Returns true if the object has the specified field or property.
        /// </summary>
        public static bool HasFieldOrProperty(this object? obj, string fieldPropName)
            => obj.HasProperty(fieldPropName) || obj.HasField(fieldPropName);
    }
}

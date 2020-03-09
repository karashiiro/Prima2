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

        /// <summary>
        /// Calculate the Levenshtein distance between two strings.
        /// </summary>
        public static int Levenshtein(string string1, string string2)
        {
            int a = string1.Length;
            int b = string2.Length;
            if (a == 0)
                return b;
            if (b == 0)
                return a;
            int costBonus = Math.Abs(a - b); // If there's a difference between the strings, that means an insert for every extra character.
            a = Math.Min(a, b); // These then can be equal since we've pulled out the difference already.
            b = a;
            int[,] matrix = new int[a, b];
            for (int k = 0; k < a; k++)
            {
                matrix[0, k] = k;
                matrix[k, 0] = k;
            }
            for (int i = 1; i < a; i++)
            {
                for (int j = 1; j < b; j++)
                {
                    if (string1[i] == string2[j])
                        matrix[i, j] = Math.Min(matrix[i - 1, j - 1], matrix[i, j - 1]);
                    else
                        matrix[i, j] = Math.Min(matrix[i - 1, j - 1], Math.Min(matrix[i, j - 1], matrix[i - 1, j])) + 1;
                }
            }
            return matrix[a - 1,b - 1];
        }

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

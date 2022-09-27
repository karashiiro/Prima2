using Prima.Game.FFXIV;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
        /// Converts a function that requires four arguments into a function that requires three arguments.
        /// </summary>
        public static Func<T2, T3, T4, TResult> Apply<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> f, T1 arg)
            => (x, y, z) => f(arg, x, y, z);

        /// <summary>
        /// Gets the absolute path of a file from its path relative to the entry assembly.
        /// </summary>
        public static string GetAbsolutePath(string relativePath)
            => Path.Combine(Assembly.GetEntryAssembly()!.Location, "..", relativePath);

        public static string GetClosestString(string input, IEnumerable<string> options)
        {
            var enumerable = options.ToList();
            var output = enumerable.First();
            foreach (var option in enumerable)
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

            return matrix[a - 1, b - 1];
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
                    outTime = TimeZoneInfo.ConvertTime(time,
                        time.Kind == DateTimeKind.Utc ? TimeZoneInfo.Utc : TimeZoneInfo.Local, tzi);
                }
                catch (ArgumentNullException)
                {
                }
                catch (TimeZoneNotFoundException)
                {
                }
                catch (InvalidTimeZoneException)
                {
                }
            }

            return (tzi, outTime);
        }

        public static bool IsUnix()
            => Environment.OSVersion.Platform == PlatformID.Unix;

        public static string HtIdString()
        {
            return IsUnix() ? "Pacific/Honolulu" : "Hawaiian Standard Time";
        }

        public static string AktIdString()
        {
            return IsUnix() ? "America/Anchorage" : "Alaskan Standard Time";
        }

        public static string PtIdString()
        {
            return IsUnix() ? "America/Los_Angeles" : "Pacific Standard Time";
        }

        public static string MtIdString()
        {
            return IsUnix() ? "America/Denver" : "Mountain Standard Time";
        }

        public static string CtIdString()
        {
            return IsUnix() ? "America/Chicago" : "Central Standard Time";
        }

        public static string EtIdString()
        {
            return IsUnix() ? "America/New_York" : "Eastern Standard Time";
        }

        public static string Capitalize(string input)
            => char.ToUpperInvariant(input[0]) + input[1..].ToLowerInvariant();

        public static string JadenCase(string input)
            => input.Split(" ").Select(Capitalize)
                .Aggregate((workingSentence, nextWord) => workingSentence + " " + nextWord);

        public static string ToAbbreviation(string input)
            => input.Split(" ").Select(word => char.ToUpperInvariant(word[0]).ToString())
                .Aggregate((str, c) => str + c);

        public static string CleanDiscordMention(string user)
        {
            var userId = user;

            while (!char.IsDigit(userId[0]))
            {
                userId = userId[1..];
            }

            while (!char.IsDigit(userId[^1]))
            {
                userId = userId[..^1];
            }

            return userId;
        }

        /// <summary>
        /// Get the value of an object property by its string name.
        /// </summary>
        public static object GetPropertyValue(this object? obj, string propName)
            => obj?.GetType().GetProperty(propName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
                ?.GetValue(obj, null);

        /// <summary>
        /// Returns true if the object has the specified property.
        /// </summary>
        public static bool HasProperty(this object? obj, string propName)
            => obj?.GetType().GetProperties().Any(pi => pi.Name == propName) ?? false;

        /// <summary>
        /// Returns true if the object has the specified field.
        /// </summary>
        public static bool HasField(this object? obj, string fieldName)
            => obj?.GetType().GetFields().Any(pi => pi.Name == fieldName) ?? false;

        /// <summary>
        /// Returns true if the object has the specified field or property.
        /// </summary>
        public static bool HasFieldOrProperty(this object? obj, string fieldPropName)
            => obj.HasProperty(fieldPropName) || obj.HasField(fieldPropName);
    }
}
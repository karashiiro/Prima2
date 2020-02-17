using System;
using System.IO;
using System.Reflection;

namespace Prima
{
    public static class Util
    {
        public static TimeSpan IncrementAverage(TimeSpan lastAverage, int lastN, TimeSpan newValue)
            => (lastN * lastAverage + newValue) / (lastN + 1);

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
    }
}

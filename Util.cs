using System;

namespace Prima
{
    public static class Util
    {
        public static TimeSpan IncrementAverage(TimeSpan lastAverage, int lastN, TimeSpan newValue)
            => (lastN * lastAverage + newValue) / (lastN + 1);

        public static Func<T2, T3, TResult> Apply<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> f, T1 arg)
            => (x, y) => f(arg, x, y);
    }
}

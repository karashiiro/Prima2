using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Prima.Extensions
{
    public static class ListExtensions
    {
        public static bool Remove<T>(this IList<T> list, Predicate<T> predicate)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    list.Remove(list[i]);
                    return true;
                }
            }
            return false;
        }

        public static int IndexOf<T>(this IList<T> list, Predicate<T> predicate)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public static IList<T> RemoveAll<T>(this IList<T> list, Predicate<T> predicate, bool overload = false)
        {
            lock (list)
            {
                var ret = new List<T>();
                for (var i = 0; i < list.Count; i++)
                {
                    if (predicate(list[i]))
                    {
                        ret.Add(list[i]);
                        list.Remove(list[i]);
                        i--;
                    }
                }
                return ret;
            }
        }
    }
}

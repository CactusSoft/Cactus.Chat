using System;
using System.Collections.Generic;
using System.Text;

namespace Cactus.Chat
{
    public static class EnumerableExtension
    {
        public static void ForEach<T>(this IEnumerable<T> enumarable, Action<T> action)
        {
            foreach (var e in enumarable)
            {
                action(e);
            }
        }
    }
}

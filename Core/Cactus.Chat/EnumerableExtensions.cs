using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cactus.Chat.Signalr
{
    public static class EnumerableExtensions
    {
        public static Task WhenAll(this IEnumerable<Task> collection)
        {
            return Task.WhenAll(collection);
        }

        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> collection)
        {
            return Task.WhenAll(collection);
        }
    }
}

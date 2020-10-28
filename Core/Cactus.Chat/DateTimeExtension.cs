using System;

namespace Cactus.Chat
{
    public static class DateTimeExtension
    {
        public static DateTime DropMilliseconds(this DateTime dt)
        {
            return dt.AddMilliseconds(-dt.Millisecond);
        }

        public static DateTime RoundToMilliseconds(this DateTime dt)
        {
            return dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerMillisecond));
        }
    }
}

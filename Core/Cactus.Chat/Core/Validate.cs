using System;
using System.Collections.Generic;

namespace Cactus.Chat.Core
{
    public static class Validate
    {
        public static void NotNull(object obj, string message = null)
        {
            if (obj == null)
            {
                if (message == null)
                {
                    throw new ArgumentException("Input object is null");
                }

                throw new ArgumentException(message);
            }
        }

        public static void NotEmptyString(string value, string message = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                if (message == null)
                {
                    throw new ArgumentException("Input string is empty or null");
                }

                throw new ArgumentException(message);
            }
        }

        public static void NotNullOrEmpty<T>(ICollection<T> collection, string message = null)
        {
            if (collection != null && collection.Count > 0)
            {
                return;
            }

            if (message != null)
            {
                throw new ArgumentException(message);
            }

            throw new ArgumentException("Empty collection");
        }
    }
}

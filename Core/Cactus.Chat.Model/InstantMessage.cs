using System;
using System.Collections.Generic;

namespace Cactus.Chat.Model
{
    public class InstantMessage
    {
        /// <summary>
        /// Message UTC timestamp. Identify the message in the chat's message stream.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// User identifier
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Message text. Optional if File is specified
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Message attachments
        /// </summary>
        public ICollection<Attachment> Attachments { get; set; }
    }

    public class Attachment
    {
        public Uri Url { get; set; }

        public Uri IconUrl { get; set; }

        public string Name { get; set; }

        public ulong? Size { get; set; }

        public string Type { get; set; }
    }
}

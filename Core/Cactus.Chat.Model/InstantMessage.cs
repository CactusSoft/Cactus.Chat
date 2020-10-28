using System;

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
        /// File content. Optional if Message is specified
        /// </summary>
        public ChatFile File { get; set; }
    }

    public class ChatFile
    {
        public Uri Url { get; set; }

        public Uri IconUrl { get; set; }

        public string Name { get; set; }

        public ulong? Size { get; set; }

        public string Type { get; set; }
    }
}

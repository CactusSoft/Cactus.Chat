using System;
using Cactus.Chat.Model;

namespace Cactus.Chat.Events
{
    /// <summary>
    /// New message to send
    /// </summary>
    public class MessageNew<T> : IMessageIdentifier where T : InstantMessage
    {
        public string UserId => Payload.Author;

        public DateTime Timestamp => Payload.Timestamp;

        public string ChatId { get; set; }

        public string ConnectionId { get; set; }

        /// <summary>
        /// The message itself
        /// </summary>
        public T Payload { get; set; }
    }
}
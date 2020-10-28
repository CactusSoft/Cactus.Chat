using System;

namespace Cactus.Chat.Events
{
    public class MessageDelivered : IMessageIdentifier
    {
        public string ChatId { get; set; }

        public string UserId { get; set; }

        public string ConnectionId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
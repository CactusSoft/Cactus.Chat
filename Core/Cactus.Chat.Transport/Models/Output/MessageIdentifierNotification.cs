using System;

namespace Cactus.Chat.Transport.Models.Output
{
    public class MessageIdentifierNotification
    {
        public string ChatId { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
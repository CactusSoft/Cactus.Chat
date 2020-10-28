using System;

namespace Cactus.Chat.Model.Dto
{
    public class MessageRead
    {
        public MessageRead(string chatId, string userId, DateTime timestamp)
        {
        }

        public string ChatId { get; set; }

        public string UserId { get; set; }

        public DateTime Timestamp { get; set; }
    }
}

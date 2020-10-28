using Cactus.Chat.Model;

namespace Cactus.Chat.Transport.Models.Output
{
    public class MessageNewNotification<T> where T : InstantMessage
    {
        public string ChatId { get; set; }
        public T Message { get; set; }
    }
}

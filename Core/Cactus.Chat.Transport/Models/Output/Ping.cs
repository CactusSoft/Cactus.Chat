using System;

namespace Cactus.Chat.Transport.Models.Output
{
    public class Ping
    {
        public DateTime Timestamp { get; set; }
        public string Executable { get; set; }
        public string ChatService { get; set; }
        public string Storage { get; set; }
        public bool IsAuthenticated { get; set; }
        public string UserId { get; set; }
    }
}

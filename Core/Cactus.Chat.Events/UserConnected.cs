namespace Cactus.Chat.Events
{
    public class UserConnected : IUserIdentifier
    {
        public string UserId { get; set; }

        public string ConnectionId { get; set; }

        public string BroadcastGroup { get; set; }
    }
}

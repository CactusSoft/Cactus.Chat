namespace Cactus.Chat.Events
{
    public class UserDisconnected : IUserIdentifier
    {
        public string UserId { get; set; }

        public string ConnectionId { get; set; }

        public string BroadcastGroup { get; set; }
    }
}

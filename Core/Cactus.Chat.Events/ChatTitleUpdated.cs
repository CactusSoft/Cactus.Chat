namespace Cactus.Chat.Events
{
    public class ChatTitleUpdated : IParticipantIdentifier
    {
        public string ChatId { get; set; }

        public string UserId { get; set; }
       
        public string ConnectionId { get; set; }

        public string Title { get; set; }
    }
}

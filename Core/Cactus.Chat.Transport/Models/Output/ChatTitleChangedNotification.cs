namespace Cactus.Chat.Transport.Models.Output
{
    public class ChatTitleChangedNotification : ParticipantIdentifierNotification
    {
        public string Title { get; set; }
    }
}
namespace Cactus.Chat.Events
{
    public class ParticipantStopTyping : IParticipantIdentifier
    {
        public string ChatId { get; set; }

        public string UserId { get; set; }

        public string ConnectionId { get; set; }
    }
}

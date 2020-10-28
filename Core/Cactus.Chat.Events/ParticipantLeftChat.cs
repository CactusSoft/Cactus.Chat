namespace Cactus.Chat.Events
{
    public class ParticipantLeftChat : IParticipantIdentifier
    {
        public string ChatId { get; set; }

        public string UserId { get; set; }

        public string ConnectionId { get; set; }
    }
}

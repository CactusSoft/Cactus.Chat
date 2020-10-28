using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Transport.Models.Output
{
    public class ParticipantAddedNotification<T> : ParticipantIdentifierNotification where T : IChatProfile
    {
        public NotificationParticipant<T> Participant { get; set; }
    }

    public class NotificationParticipant<T> where T : IChatProfile
    {
        public string Id { get; set; }
        public T Profile { get; set; }
    }
}
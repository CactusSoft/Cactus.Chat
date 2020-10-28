using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Events
{
    public class ParticipantAdded<T> : IParticipantIdentifier
        where T : IChatProfile
    {
        public string ChatId { get; set; }

        public string UserId { get; set; }

        public string ConnectionId { get; set; }

        public IContextUser<T> Participant { get; set; }
    }
}

using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Events
{
    public class ChatCreated<T1, T2, T3> : IParticipantIdentifier
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        public string ChatId => Chat.Id;

        public string UserId { get; set; }
       
        public string ConnectionId { get; set; }

        public T1 Chat { get; set; }
        
    }
}

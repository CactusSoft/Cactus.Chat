using System;
using Cactus.Chat.Model.Base;
using MongoDB.Bson.Serialization.Attributes;

namespace Cactus.Chat.Model
{
    [BsonIgnoreExtraElements(Inherited = true)]
    public class ChatParticipant<T> : IContextUser<T>
        where T : IChatProfile
    {
        public string Id { get; set; }

        public DateTime? ReadOn { get; set; }

        public DateTime? DeliveredOn { get; set; }
        
        public DateTime? LastMessageOn { get; set; }

        public bool IsMuted { get; set; }
        
        public bool IsDeleted { get; set; }

        public bool HasLeft { get; set; }

        public T Profile { get; set; }
    }
}
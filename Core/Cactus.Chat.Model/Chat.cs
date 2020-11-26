using System;
using System.Collections.Generic;
using Cactus.Chat.Model.Base;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Cactus.Chat.Model
{
    [BsonIgnoreExtraElements(Inherited = true)]
    public class Chat<T1, T2>
        where T1 : InstantMessage
        where T2 : IChatProfile
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Title { get; set; }

        public IList<ChatParticipant<T2>> Participants { get; set; }

        public IList<T1> Messages { get; set; }

        public DateTime StartedOn { get; set; }

        public string StartedBy { get; set; }

        public DateTime LastActivityOn { get; set; }

        public int MessageCount { get; set; }
    }
}

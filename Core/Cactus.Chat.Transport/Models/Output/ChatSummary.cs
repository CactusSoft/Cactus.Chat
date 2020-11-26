using System;
using System.Collections.Generic;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Transport.Models.Output
{
    public class ChatSummary<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int MessageCount { get; set; }
        public string StartedBy { get; set; }
        public DateTime StartedOn { get; set; }
        public DateTime? ReadOn { get; set; }
        public T2 LastMessage { get; set; }
        public ICollection<ParticipantSummary<T3>> Participants { get; set; }
    }

    public class ParticipantSummary<T3> where T3 : IChatProfile
    {
        public string Id { get; set; }
        public T3 Profile { get; set; }
        public bool IsDeleted { get; set; }
        public bool HasLeft { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? ReadOn { get; set; }
        public DateTime? DeliveredOn { get; set; }
        public DateTime? LastMessageOn { get; set; }
    }
}

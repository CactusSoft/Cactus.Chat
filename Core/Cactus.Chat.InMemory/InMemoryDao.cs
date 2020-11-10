using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Storage
{
    public class InMemoryDao<T1, T2, T3> : IChatDao<T1, T2, T3>
        where T1 : Chat<T2, T3>, new()
        where T2 : InstantMessage, new()
        where T3 : IChatProfile, new()
    {
        private static readonly IList<T1> ChatList = new List<T1>();

        public Task<IEnumerable<T1>> GetUserChatList(string userId, Expression<Func<T1, bool>> filter = null)
        {
            var userChats = ChatList
                .Where(e => e.Participants != null &&
                            e.Participants.Any(p => p.Id == userId && !p.HasLeft && !p.IsDeleted));
            if (filter != null)
                userChats = userChats.Where(filter.Compile());
            return Task.FromResult(userChats.Select(Copy));
        }

        public Task<T1> Get(string chatId)
        {
            return Task.FromResult(Copy(ChatList.First(e => e.Id == chatId)));
        }

        public Task<T1> FindChatWithParticipants(string userId1, string userId2)
        {
            return Task.FromResult(Copy(ChatList.FirstOrDefault(e =>
                e.Participants != null &&
                e.Participants.Count == 2 &&
                e.Participants.Any(p => p.Id == userId1) &&
                e.Participants.Any(p => p.Id == userId2)
            )));
        }

        public Task Create(T1 chat)
        {
            chat.Id = Guid.NewGuid().ToString("N");
            ChatList.Add(Copy(chat));
            return Task.FromResult(0);
        }

        public Task AddMessage(string chatId, T2 msg)
        {
            var chat = ChatList.FirstOrDefault(e => e.Id == chatId);
            if (chat == null)
                throw new ArgumentException("chatId");
            if (chat.Messages == null)
                chat.Messages = new List<T2>();
            chat.Messages.Add(Copy(msg));
            return Task.FromResult(0);
        }

        public Task SetParticipantRead(string chatId, string userId, DateTime timestamp)
        {
            var chat = ChatList.FirstOrDefault(e => e.Id == chatId);
            if (chat == null)
                throw new ArgumentException("chatId");
            if (chat.Participants == null)
                throw new ArgumentException("chat has no participants");
            var usr = chat.Participants.FirstOrDefault(e => e.Id == userId);
            if (usr == null)
                throw new ArgumentException("userId");
            usr.ReadOn = timestamp;
            return Task.FromResult(0);
        }

        public Task<IEnumerable<string>> SetParticipantReadAll(string userId, DateTime timestamp)
        {
            var result = Task.FromResult(SetParticipantReadAllSync(userId, timestamp));
            return result;
        }

        private IEnumerable<string> SetParticipantReadAllSync(string userId, DateTime timestamp)
        {
            foreach (var chat in ChatList)
            {
                var usr = chat?.Participants?.FirstOrDefault(p => p.Id == userId);
                if (usr != null)
                {
                    usr.ReadOn = timestamp;
                    yield return chat.Id;
                }
            }
        }

        public Task SetParticipantDelivered(string chatId, string userId, DateTime timestamp)
        {
            var chat = ChatList.FirstOrDefault(e => e.Id == chatId);
            if (chat == null)
                throw new ArgumentException("chatId");
            if (chat.Participants == null)
                throw new ArgumentException("chat has no participants");
            var usr = chat.Participants.FirstOrDefault(e => e.Id == userId);
            if (usr == null)
                throw new ArgumentException("userId");
            usr.DeliveredOn = timestamp;
            return Task.FromResult(0);
        }

        public Task SetParticipantLeft(string chatId, string userId)
        {
            return SetParticipantLeft(chatId, userId, true);
        }

        public Task SetParticipantLeft(string chatId, string userId, bool hasLeft)
        {
            var chat = ChatList.FirstOrDefault(e => e.Id == chatId);
            if (chat == null)
                throw new ArgumentException("chatId");
            if (chat.Participants == null)
                throw new ArgumentException("chat has no participants");
            var usr = chat.Participants.FirstOrDefault(e => e.Id == userId);
            if (usr == null)
                throw new ArgumentException("userId");
            usr.HasLeft = hasLeft;
            return Task.FromResult(0);
        }

        public Task SetParticipantDeleted(string userId)
        {
            return SetParticipantDeleted(userId, true);
        }

        public Task SetParticipantDeleted(string userId, bool isDeleted)
        {
            foreach (var chat in ChatList)
            {
                var participant = chat.Participants.FirstOrDefault(p => p.Id == userId);
                if (participant != null)
                    participant.IsDeleted = isDeleted;
            }

            return Task.FromResult(0);
        }

        public Task SetParticipants(string chatId, IList<ChatParticipant<T3>> participants)
        {
            var chat = ChatList.FirstOrDefault(e => e.Id == chatId);
            if (chat == null)
                throw new ArgumentException("chatId");
            chat.Participants = participants.Select(Copy).ToList();
            return Task.FromResult(0);
        }

        public Task<IList<ChatParticipant<T3>>> GetParticipants(string chatId)
        {
            var chat = ChatList.FirstOrDefault(e => e.Id == chatId);
            if (chat == null)
                throw new ArgumentException("chatId");
            IList<ChatParticipant<T3>> res = chat.Participants.Select(Copy).ToList();
            return Task.FromResult(res);
        }

        public Task SetTitle(string chatId, string title)
        {
            var chat = ChatList.FirstOrDefault(e => e.Id == chatId);
            if (chat == null)
                throw new ArgumentException("chatId");
            chat.Title = title;
            return Task.FromResult(0);
        }

        public Task UpdateProfile(string userId, T3 profile)
        {
            foreach (var chat in ChatList)
            {
                var participant = chat.Participants.FirstOrDefault(p => !p.IsDeleted && !p.HasLeft && p.Id == userId);
                if (participant != null)
                    participant.Profile = profile;
            }

            return Task.FromResult(0);
        }

        public Task<string> GetInfo()
        {
            return Task.FromResult(
                $"InMemory, assembly version {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");
        }

        private ChatParticipant<T3> Copy(ChatParticipant<T3> e)
        {
            if (e == null)
                return null;

            return new ChatParticipant<T3>
            { 
                IsMuted = e.IsMuted,
                LastMessageOn = e.LastMessageOn,
                HasLeft = e.HasLeft,
                Id = e.Id,
                DeliveredOn = e.DeliveredOn,
                IsDeleted = e.IsDeleted,
                ReadOn = e.ReadOn,
                Profile = e.Profile
            };
        }

        private T2 Copy(T2 e)
        {
            if (e == null)
                return null;
            return new T2
            {
                Message = e.Message,
                Attachments = e.Attachments?.Select(Copy).ToArray(),
                Author = e.Author,
                Type = e.Type,
                Timestamp = e.Timestamp
            };
        }

        private Attachment Copy(Attachment e)
        {
            if (e == null)
                return null;
            return new Attachment {Name = e.Name, IconUrl = e.IconUrl, Size = e.Size, Url = e.Url, Type = e.Type};
        }

        private T1 Copy(T1 e)
        {
            if (e == null)
                return null;
            return new T1
            {
                Messages = e.Messages?.Select(Copy).ToList(),
                Id = e.Id,
                Participants = e.Participants?.Select(Copy).ToList(),
                Title = e.Title,
                LastActivity = e.LastActivity,
                MessageCount = e.MessageCount,
                StartedBy = e.StartedBy,
                StartedOn = e.StartedOn
            };
        }
    }
}
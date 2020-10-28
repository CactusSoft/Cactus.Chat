using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Events;
using Cactus.Chat.External;
using Cactus.Chat.Logging;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Storage;

namespace Cactus.Chat.Core
{
    public class ChatService<T1, T2, T3> : IChatService<T1, T2, T3>, IDisposable
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        private readonly ILog log = LogProvider.GetLogger("Cactus.Chat.Core.ChatService");
        private readonly ISecurityManager<T1, T2, T3> securityManager;
        private readonly IUserProfileProvider<T3> userProfileProvider;
        private readonly IChatDao<T1, T2, T3> storage;
        private readonly IEventHub bus;
        private readonly SemaphoreSlim createChatLocker = new SemaphoreSlim(1);

        public ChatService(
            ISecurityManager<T1, T2, T3> securityManager,
            IUserProfileProvider<T3> userProfileProvider,
            IChatDao<T1, T2, T3> storage,
            IEventHub bus)
        {
            this.securityManager = securityManager;
            this.userProfileProvider = userProfileProvider;
            this.storage = storage;
            this.bus = bus;
        }

        public async Task<T1> StartChat(IAuthContext me, T1 chat)
        {
            ValidateNewChat(chat);
            await FillUpNewChatFields(me, chat);
            await securityManager.TryStart(me, chat);
            try
            {
                await createChatLocker.WaitAsync();
                if (chat.Participants.Count == 2)
                {
                    // Redirect to existing 1-to-1 chat if exists
                    var duplicate = await storage.FindChatWithParticipants(chat.Participants[0].Id, chat.Participants[1].Id);
                    if (duplicate != null)
                    {
                        log.Debug("Duplication found");
                        await ReviveP2PChat(duplicate);

                        foreach (var m in chat.Messages)
                        {
                            await storage.AddMessage(duplicate.Id, m);
                            await PushNewMessage(duplicate.Id, me, m);
                        }
                        return await storage.Get(duplicate.Id);
                    }
                }

                await storage.Create(chat);
                foreach (var message in chat.Messages)
                {
                    await PushNewMessage(chat.Id, me, message);
                }

                return chat;
            }
            finally
            {
                createChatLocker.Release();
            }
        }

        public async Task SendMessage(IAuthContext me, string chatId, T2 message)
        {
            Validate.NotEmptyString(chatId);
            Validate.NotNull(message);
            if (string.IsNullOrWhiteSpace(message.Message) && message.File == null)
            {
                throw new ArgumentException("Message could not be empty");
            }

            var chat = await storage.Get(chatId).ConfigureAwait(false);
            await securityManager.TrySendMessage(me, chatId, message, new Lazy<T1>(() => chat));

            await ReviveP2PChat(chat);

            log.DebugFormat($"Send message to chat {chatId}, msg: {message.Message}");
            var userId = me.GetUserId();
            message.Timestamp = DateTime.UtcNow.RoundToMilliseconds();
            message.Author = userId;

            await storage.AddMessage(chatId, message);
            await PushNewMessage(chatId, me, message);
            log.Debug("Message added to chat successfully");
        }

        public async Task<IEnumerable<T2>> GetMessageHistory(IAuthContext me, string chatId, DateTime from, DateTime to, int count, bool moveBackward)
        {
            log.Debug("Get message history");
            Validate.NotEmptyString(chatId);
            @from = @from.ToUniversalTime();
            to = to.ToUniversalTime();
            var userId = me.GetUserId();
            var chat = await storage.Get(chatId);
            await securityManager.TryRead(me, chatId, new Lazy<T1>(() => chat));

            if (!chat.Participants.Any(e => e.Id == userId && !e.HasLeft && !e.IsDeleted))
            {
                log.Warn("Current participant has not found in chat or disabled");
                return Enumerable.Empty<T2>();
            }

            var source = moveBackward ? chat.Messages.ReverseEnumerable() : chat.Messages;
            source = moveBackward ?
                source.Where(m => m.Timestamp < @from && m.Timestamp >= to) :
                source.Where(m => m.Timestamp > @from && m.Timestamp <= to);
            source = source.Take(count);
            if (moveBackward)
            {
                source = source.Reverse();
            }
            return source;
        }

        public async Task<IEnumerable<T1>> Get(IAuthContext me, Expression<Func<T1, bool>> expression = null)
        {
            log.Debug("Get chat list");
            var userId = me.GetUserId();
            return await storage.GetUserChatList(userId, expression);
        }

        public async Task<T1> Get(IAuthContext me, string id)
        {
            Validate.NotEmptyString(id);
            var chat = await storage.Get(id);
            var lazyChat = new Lazy<T1>(() => chat);

            await securityManager.TryRead(me, id, lazyChat);

            var userId = me.GetUserId();
            if (chat.Participants.Any(e => e.Id == userId && !e.HasLeft && !e.IsDeleted))
            {
                return chat;
            }

            throw new ArgumentException("Nothing found");
        }

        public async Task<DateTime> MarkRead(IAuthContext me, string chatId, DateTime timestamp)
        {
            Validate.NotEmptyString(chatId);

            var userId = me.GetUserId();
            await storage.SetParticipantRead(chatId, userId, timestamp);
            await bus.FireEvent(new MessageRead
            {
                ChatId = chatId,
                ConnectionId = me.ConnectionId,
                Timestamp = timestamp,
                UserId = userId
            });
            return timestamp;
        }

        public async Task<DateTime> MarkReadBulk(IAuthContext me, DateTime timestamp)
        {
            var userId = me.GetUserId();
            var chatsWithReadMessagesIds = await storage.SetParticipantReadAll(userId, timestamp);
            var publishMessageReadTasks = chatsWithReadMessagesIds.Select(chatId => bus.FireEvent(new MessageRead
            {
                ChatId = chatId,
                ConnectionId = me.ConnectionId,
                Timestamp = timestamp,
                UserId = userId
            }));

            await Task.WhenAll(publishMessageReadTasks);
            return timestamp;
        }

        public async Task<DateTime> MarkDelivered(IAuthContext me, string chatId, DateTime timestamp)
        {
            Validate.NotEmptyString(chatId);

            var userId = me.GetUserId();
            await storage.SetParticipantDelivered(chatId, userId, timestamp);
            await bus.FireEvent(new MessageDelivered
            {
                ChatId = chatId,
                ConnectionId = me.ConnectionId,
                Timestamp = timestamp,
                UserId = userId
            });
            return timestamp;
        }

        public async Task LeaveChat(IAuthContext me, string chatId)
        {
            Validate.NotEmptyString(chatId);

            var userId = me.GetUserId();
            await storage.SetParticipantLeft(chatId, userId);
            await bus.FireEvent(new ParticipantLeftChat
            {
                ChatId = chatId,
                UserId = userId,
                ConnectionId = me.ConnectionId
            });
        }

        public async Task AddParticipants(IAuthContext me, string chatId, IEnumerable<string> participants)
        {
            Validate.NotNull(participants);
            Validate.NotEmptyString(chatId);

            var ids = participants.Distinct().ToList();
            ids.ForEach(e => Validate.NotEmptyString(e, "Ids"));
            var chat = await storage.Get(chatId);

            var userId = me.GetUserId();
            if (!chat.Participants.Any(e => e.Id == userId && !e.IsDeleted && !e.HasLeft))
            {
                throw new SecurityException("You are not active chat participant");
            }

            var needUpdate = false;
            var events = new List<ParticipantAdded<T3>>(ids.Count);
            foreach (var participantId in ids)
            {
                var participant = chat.Participants.FirstOrDefault(e => e.Id == participantId);
                if (participant != null)
                {
                    if (!participant.HasLeft)
                    {
                        log.Debug("Participan is already in the chat, nothing to add.");
                        continue;
                    }

                    log.Debug($"Participant {participant.Id} has marked as active");
                    participant.HasLeft = false;
                    needUpdate = true;
                }
                else
                {
                    await securityManager.TryAddParticipant(me, chatId, participantId, new Lazy<T1>(() => chat));
                    log.Debug("Add new participant");
                    var user = await userProfileProvider.Get(participantId);
                    chat.Participants.Add(new ChatParticipant<T3>
                    {
                        Id = participantId,
                        Profile = user.Profile,
                        IsDeleted = user.IsDeleted
                    });
                    events.Add(new ParticipantAdded<T3>
                    {
                        ChatId = chatId,
                        Participant = user,
                        UserId = me.GetUserId(),
                        ConnectionId = me.ConnectionId
                    });
                    needUpdate = true;
                }
            }

            if (needUpdate)
            {
                await storage.SetParticipants(chatId, chat.Participants);
                events.ForEach(async e => await bus.FireEvent(e));
            }
        }

        public async Task<IList<ChatParticipant<T3>>> GetParticipants(string chatId)
        {
            Validate.NotEmptyString(chatId);
            return await storage.GetParticipants(chatId);
        }

        public async Task ChangeTitle(IAuthContext me, string chatId, string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("title");
            }

            // var chat = GetLazyChat(chatId);
            // TODO:check if user is able to change title

            await storage.SetTitle(chatId, title);
            await bus.FireEvent(new ChatTitleUpdated
            {
                ChatId = chatId,
                UserId = me.GetUserId(),
                ConnectionId = me.ConnectionId,
                Title = title
            });
            log.Debug("Chat title updated successfully");
        }

        public Task<string> GetStorageInfo()
        {
            return storage.GetInfo();
        }

        public Task ParticipantStartTyping(IAuthContext me, string chatId)
        {
            if (me == null || chatId == null)
            {
                log.Warn("ParticipantStartTyping has been called with incorrect params, do nothing.");
                return Task.CompletedTask;
            }

            return bus.FireEvent(new ParticipantStartTyping
            {
                ChatId = chatId,
                ConnectionId = me.ConnectionId,
                UserId = me.GetUserId()
            });
        }

        public Task ParticipantStopTyping(IAuthContext me, string chatId)
        {
            if (me == null || chatId == null)
            {
                log.Warn("ParticipantStopTyping has been called with incorrect params, do nothing.");
                return Task.CompletedTask;
            }

            return bus.FireEvent(new ParticipantStopTyping
            {
                ChatId = chatId,
                ConnectionId = me.ConnectionId,
                UserId = me.GetUserId()
            });
        }

        protected virtual void ValidateNewChat(T1 chat)
        {
            Validate.NotNull(chat);
            Validate.NotNullOrEmpty(chat.Participants, "Chat has no participants");
            chat.Participants.ForEach(e => Validate.NotEmptyString(e.Id, "Empty participant id"));
        }

        protected virtual async Task PushNewMessage(string chatId, IAuthContext me, T2 message)
        {
            await bus.FireEvent(new MessageNew<T2>
            {
                Payload = message,
                ChatId = chatId,
                ConnectionId = me.ConnectionId,
            });
        }

        protected virtual async Task FillUpNewChatFields(IAuthContext auth, T1 chat)
        {
            // Fill up other fields
            // Add current contact to participant list
            var now = DateTime.UtcNow.RoundToMilliseconds();
            var authorId = auth.GetUserId();

            foreach (var participant in chat.Participants)
            {
                var user = await userProfileProvider.Get(participant.Id);
                participant.Profile = user.Profile;
                participant.IsDeleted = user.IsDeleted;
            }

            if (chat.Participants.All(x => x.Id != authorId))
            {
                var author = await userProfileProvider.Get(auth);
                chat.Participants.Add(new ChatParticipant<T3>
                {
                    Id = authorId,
                    ReadOn = now,
                    Profile = author.Profile,
                    IsDeleted = author.IsDeleted
                });
            }

            chat.StartedOn = now;
            chat.StartedBy = authorId;
            chat.LastActivity = now;

            if (chat.Messages != null)
            {
                chat.MessageCount = chat.Messages.Count;
                chat.Messages.ForEach(x =>
                {
                    x.Timestamp = chat.StartedOn;
                    x.Author = authorId;
                });
            }
            else
            {
                chat.Messages = new List<T2>(0);
            }
        }

        protected virtual async Task ReviveP2PChat(T1 chat)
        {
            //Revive P2P chat if need
            if (chat.Participants.Count == 2 && chat.Participants.Any(e => e.HasLeft))
            {
                if (chat.Participants.Any(e => e.IsDeleted))
                {
                    log.WarnFormat("SendMessage: one or more user marked as deleted in p2p chat (id:{0}). Throws exception.", chat.Id);
                    throw new ArgumentException("User is deleted");
                }

                log.DebugFormat("P2P chat detected, id:{0}. One or both users left the chat. Need to revive.", chat.Id);
                foreach (var p in chat.Participants)
                {
                    if (p.HasLeft)
                    {
                        await storage.SetParticipantLeft(chat.Id, p.Id, false).ConfigureAwait(false);
                        p.HasLeft = false;
                    }
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    createChatLocker.Dispose();
                }
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

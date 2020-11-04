using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Events;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Storage;
using Microsoft.Extensions.Logging;

namespace Cactus.Chat.Core
{
    public class ChatService<T1, T2, T3> : IChatService<T1, T2, T3>, IDisposable
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        private readonly ISecurityManager<T1, T2, T3> _securityManager;
        private readonly IUserProfileProvider<T3> _userProfileProvider;
        private readonly IChatDao<T1, T2, T3> _storage;
        private readonly IEventHub _bus;
        private readonly ILogger _log;
        private readonly SemaphoreSlim _createChatLocker = new SemaphoreSlim(1);

        public ChatService(
            ISecurityManager<T1, T2, T3> securityManager,
            IUserProfileProvider<T3> userProfileProvider,
            IChatDao<T1, T2, T3> storage,
            IEventHub bus,
            ILogger<ChatService<T1,T2,T3>> log)
        {
            _securityManager = securityManager;
            _userProfileProvider = userProfileProvider;
            _storage = storage;
            _bus = bus;
            _log = log;
        }

        public async Task<T1> StartChat(IAuthContext me, T1 chat)
        {
            ValidateNewChat(chat);
            await FillUpNewChatFields(me, chat);
            await _securityManager.TryStart(me, chat);
            try
            {
                await _createChatLocker.WaitAsync();
                if (chat.Participants.Count == 2)
                {
                    // Redirect to existing 1-to-1 chat if exists
                    var duplicate =
                        await _storage.FindChatWithParticipants(chat.Participants[0].Id, chat.Participants[1].Id);
                    if (duplicate != null)
                    {
                        _log.LogDebug("Duplication found");
                        await ReviveP2PChat(duplicate);

                        foreach (var m in chat.Messages)
                        {
                            await _storage.AddMessage(duplicate.Id, m);
                            await PushNewMessage(duplicate.Id, me, m);
                        }

                        return await _storage.Get(duplicate.Id);
                    }
                }

                await _storage.Create(chat);
                foreach (var message in chat.Messages)
                {
                    await PushNewMessage(chat.Id, me, message);
                }

                return chat;
            }
            finally
            {
                _createChatLocker.Release();
            }
        }

        public async Task SendMessage(IAuthContext me, string chatId, T2 message)
        {
            Validate.NotEmptyString(chatId);
            Validate.NotNull(message);
            if (string.IsNullOrWhiteSpace(message.Message) && message.Attachments == null)
            {
                throw new ArgumentException("Message could not be empty");
            }

            var chat = await _storage.Get(chatId).ConfigureAwait(false);
            await _securityManager.TrySendMessage(me, chatId, message, new Lazy<T1>(() => chat));

            await ReviveP2PChat(chat);

            _log.LogDebug("Send message to chat {chat_id}, msg: {message}", chatId, message.Message.Secure());
            var userId = me.GetUserId();
            message.Timestamp = DateTime.UtcNow.RoundToMilliseconds();
            message.Author = userId;

            await _storage.AddMessage(chatId, message);
            await PushNewMessage(chatId, me, message);
            _log.LogDebug("Message added to chat successfully");
        }

        public async Task<IEnumerable<T2>> GetMessageHistory(IAuthContext me, string chatId, DateTime from, DateTime to,
            int count, bool moveBackward)
        {
            _log.LogDebug("Get message history");
            Validate.NotEmptyString(chatId);
            @from = @from.ToUniversalTime();
            to = to.ToUniversalTime();
            var userId = me.GetUserId();
            var chat = await _storage.Get(chatId);
            await _securityManager.TryRead(me, chatId, new Lazy<T1>(() => chat));

            if (!chat.Participants.Any(e => e.Id == userId && !e.HasLeft && !e.IsDeleted))
            {
                _log.LogWarning("Current participant has not found in chat or disabled");
                return Enumerable.Empty<T2>();
            }

            var source = moveBackward ? chat.Messages.ReverseEnumerable() : chat.Messages;
            source = moveBackward
                ? source.Where(m => m.Timestamp < @from && m.Timestamp >= to)
                : source.Where(m => m.Timestamp > @from && m.Timestamp <= to);
            source = source.Take(count);
            if (moveBackward)
            {
                source = source.Reverse();
            }

            return source;
        }

        public async Task<IEnumerable<T1>> Get(IAuthContext me, Expression<Func<T1, bool>> expression = null)
        {
            _log.LogDebug("Get chat list");
            var userId = me.GetUserId();
            return await _storage.GetUserChatList(userId, expression);
        }

        public async Task<T1> Get(IAuthContext me, string id)
        {
            Validate.NotEmptyString(id);
            var chat = await _storage.Get(id);
            var lazyChat = new Lazy<T1>(() => chat);

            await _securityManager.TryRead(me, id, lazyChat);

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
            await _storage.SetParticipantRead(chatId, userId, timestamp);
            await _bus.FireEvent(new MessageRead
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
            var chatsWithReadMessagesIds = await _storage.SetParticipantReadAll(userId, timestamp);
            var publishMessageReadTasks = chatsWithReadMessagesIds.Select(chatId => _bus.FireEvent(new MessageRead
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
            await _storage.SetParticipantDelivered(chatId, userId, timestamp);
            await _bus.FireEvent(new MessageDelivered
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
            await _storage.SetParticipantLeft(chatId, userId);
            await _bus.FireEvent(new ParticipantLeftChat
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
            var chat = await _storage.Get(chatId);

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
                        _log.LogDebug("Participant is already in the chat, nothing to add.");
                        continue;
                    }

                    _log.LogDebug("Participant {user_id} has marked as active", participant.Id);
                    participant.HasLeft = false;
                    needUpdate = true;
                }
                else
                {
                    await _securityManager.TryAddParticipant(me, chatId, participantId, new Lazy<T1>(() => chat));
                    _log.LogDebug("Add new participant");
                    var user = await _userProfileProvider.Get(participantId);
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
                await _storage.SetParticipants(chatId, chat.Participants);
                events.ForEach(async e => await _bus.FireEvent(e));
            }
        }

        public async Task<IList<ChatParticipant<T3>>> GetParticipants(string chatId)
        {
            Validate.NotEmptyString(chatId);
            return await _storage.GetParticipants(chatId);
        }

        public async Task ChangeTitle(IAuthContext me, string chatId, string title)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentException("title");
            }

            // var chat = GetLazyChat(chatId);
            // TODO:check if user is able to change title

            await _storage.SetTitle(chatId, title);
            await _bus.FireEvent(new ChatTitleUpdated
            {
                ChatId = chatId,
                UserId = me.GetUserId(),
                ConnectionId = me.ConnectionId,
                Title = title
            });
            _log.LogDebug("Chat title updated successfully");
        }

        public Task<string> GetStorageInfo()
        {
            return _storage.GetInfo();
        }

        public Task ParticipantStartTyping(IAuthContext me, string chatId)
        {
            if (me == null || chatId == null)
            {
                _log.LogWarning("ParticipantStartTyping has been called with incorrect params, do nothing.");
                return Task.CompletedTask;
            }

            return _bus.FireEvent(new ParticipantStartTyping
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
                _log.LogWarning("ParticipantStopTyping has been called with incorrect params, do nothing.");
                return Task.CompletedTask;
            }

            return _bus.FireEvent(new ParticipantStopTyping
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
            await _bus.FireEvent(new MessageNew<T2>
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
                var user = await _userProfileProvider.Get(participant.Id);
                participant.Profile = user.Profile;
                participant.IsDeleted = user.IsDeleted;
            }

            if (chat.Participants.All(x => x.Id != authorId))
            {
                var author = await _userProfileProvider.Get(auth);
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
                    _log.LogWarning(
                        "SendMessage: one or more user marked as deleted in p2p chat, chat_id: {chat_id}. Throws exception.",
                        chat.Id);
                    throw new ArgumentException("User is deleted");
                }

                _log.LogDebug("P2P chat detected, id:{chat_id}. One or both users left the chat. Need to revive.", chat.Id);
                foreach (var p in chat.Participants)
                {
                    if (p.HasLeft)
                    {
                        await _storage.SetParticipantLeft(chat.Id, p.Id, false).ConfigureAwait(false);
                        p.HasLeft = false;
                    }
                }
            }
        }

        #region IDisposable Support

        private bool _disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _createChatLocker.Dispose();
                }

                _disposedValue = true;
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
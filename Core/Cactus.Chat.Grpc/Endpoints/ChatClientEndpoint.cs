using System;
using System.Threading.Tasks;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Transport;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cactus.Chat.Grpc.Endpoints
{
    public class ChatClientEndpoint : IChatClientEndpoint
    {
        private readonly IServerStreamWriter<Notification> _responseStream;
        private readonly ILogger<ChatClientEndpoint> _log;

        public ChatClientEndpoint(
            IServerStreamWriter<Notification> responseStream,
            ILogger<ChatClientEndpoint> log)
        {
            _responseStream = responseStream;
            _log = log;
        }

        public Task MessageRead(string chatId, string userId, DateTime timestamp)
        {
            _log.LogDebug("Send messageRead event, chat: {chat_id}, user: {user_id}, timestamp: {timestamp}", chatId,
                userId, timestamp);
            var notification = new Notification
            {
                MessageRead = new MessageRead
                {
                    Timestamp = Timestamp.FromDateTime(timestamp),
                    ChatId = chatId,
                    UserId = userId
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task MessageDelivered(string chatId, string userId, DateTime timestamp)
        {
            _log.LogDebug("Send messageDelivered event, chat: {chat_id}, user: {user_id}, timestamp: {timestamp}",
                chatId,
                userId, timestamp);
            var notification = new Notification
            {
                MessageDelivered = new MessageDelivered
                {
                    Timestamp = Timestamp.FromDateTime(timestamp),
                    ChatId = chatId,
                    UserId = userId
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task MessageNew<T>(string chatId, T message) where T : Chat.Model.InstantMessage
        {
            _log.LogDebug("Send messageNew event, chat: {chat_id}", chatId);
            var notification = new Notification
            {
                MessageNew = new MessageNew
                {
                    ChatId = chatId,
                    Message = new InstantMessage
                    {
                        Author = message.Author,
                        Message = message.Message,
                        Timestamp = Timestamp.FromDateTime(message.Timestamp),
                    }
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task ParticipantAdded<T>(string chatId, string userId, string participantId, T profile)
            where T : IChatProfile
        {
            _log.LogDebug("Send participantAdded event, chat: {chat_id}, user: {user_id}", chatId, userId);
            var notification = new Notification
            {
                ParticipantAdded = new ParticipantAdded
                {
                    ChatId = chatId,
                    UserId = userId,
                    ParticipantId = participantId,
                    Profile = new UserChatProfile
                    {
                        Avatar = profile.Avatar?.ToString()
                    }
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task ParticipantLeft(string chatId, string userId)
        {
            _log.LogDebug("Send participantLeft event, chat: {chat_id}, user: {user_id}", chatId, userId);
            var notification = new Notification
            {
                ParticipantLeft = new ParticipantLeft()
                {
                    ChatId = chatId,
                    UserId = userId
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task ParticipantStartTyping(string chatId, string userId)
        {
            _log.LogDebug("Send participantStartTyping event, chat: {chat_id}, user: {user_id}", chatId, userId);
            var notification = new Notification
            {
                ParticipantStartTyping = new ParticipantStartTyping()
                {
                    ChatId = chatId,
                    UserId = userId
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task ParticipantStopTyping(string chatId, string userId)
        {
            _log.LogDebug("Send participantStopTyping event, chat: {chat_id}, user: {user_id}", chatId, userId);
            var notification = new Notification
            {
                ParticipantStopTyping = new ParticipantStopTyping()
                {
                    ChatId = chatId,
                    UserId = userId
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task ChatTitleChanged(string chatId, string userId, string title)
        {
            _log.LogDebug("Send participantStopTyping event, chat: {chat_id}, user: {user_id}", chatId, userId);
            var notification = new Notification
            {
                ChatTitleChanged = new ChatTitleChanged()
                {
                    ChatId = chatId,
                    UserId = userId,
                    Title = title
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task UserConnected(string userId)
        {
            _log.LogDebug("Send userConnected event, user: {user_id}", userId);
            var notification = new Notification
            {
                UserConnected = new UserConnected()
                {
                    UserId = userId
                }
            };
            return _responseStream.WriteAsync(notification);
        }

        public Task UserDisconnected(string userId)
        {
            _log.LogDebug("Send userDisconnected event, user: {user_id}", userId);
            var notification = new Notification
            {
                UserDisconnected = new UserDisconnected()
                {
                    UserId = userId
                }
            };
            return _responseStream.WriteAsync(notification);
        }
    }
}
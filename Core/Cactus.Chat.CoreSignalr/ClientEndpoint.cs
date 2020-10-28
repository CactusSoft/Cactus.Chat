using System;
using System.Threading.Tasks;
using Cactus.Chat.Logging;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Transport;
using Cactus.Chat.Transport.Models.Output;
using Microsoft.AspNetCore.SignalR;

namespace Cactus.Chat.Signalr
{
    public class ClientEndpoint<T> : IChatClientEndpoint where T: Hub
    {
        private readonly ILog _log = LogProvider.GetLogger(typeof(ClientEndpoint<>));
        private readonly IHubContext<T> _hub;
        private readonly string _connectionId;

        public ClientEndpoint(IHubContext<T> hubContext, string connectionId)
        {
            _hub = hubContext;
            _connectionId = connectionId;
        }

        protected Task SendAsync(string method, object model)
        {
            try
            {
                return _hub.Clients.Client(_connectionId).SendAsync(method, model);
            }
            catch (Exception ex)
            {
                _log.Error("Something goes wrong while sending {0} to {1}, the exception is {2}", method, _connectionId, ex);
                throw;
            }
        }

        public Task MessageRead(string chatId, string userId, DateTime timestamp)
        {
            return SendAsync("messageRead", new MessageIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId,
                Timestamp = timestamp
            });
        }

        public Task MessageDelivered(string chatId, string userId, DateTime timestamp)
        {
            return SendAsync("messageDelivered", new MessageIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId,
                Timestamp = timestamp
            });
        }

        public Task MessageNew<T1>(string chatId, T1 message) where T1 : InstantMessage
        {
            return SendAsync("messageNew", new MessageNewNotification<T1>
            {
                ChatId = chatId,
                Message = message
            });
        }

        public Task ParticipantAdded<T1>(string chatId, string userId, string participantId, T1 profile) where T1 : IChatProfile
        {
            return SendAsync("participantAdded", new ParticipantAddedNotification<T1>()
            {
                ChatId = chatId,
                UserId = userId,
                Participant = new NotificationParticipant<T1>
                {
                    Id= participantId,
                    Profile = profile
                }
            });
        }

        public Task ParticipantLeft(string chatId, string userId)
        {
            return SendAsync("participantLeft", new ParticipantIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId
            });
        }

        public Task ParticipantStartTyping(string chatId, string userId)
        {
            return SendAsync("participantStartTyping", new ParticipantIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId
            });
        }

        public Task ParticipantStopTyping(string chatId, string userId)
        {
            return SendAsync("participantStopTyping", new ParticipantIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId
            });
        }

        public Task ChatTitleChanged(string chatId, string userId, string title)
        {
            return SendAsync("chatTitleChanged", new ChatTitleChangedNotification()
            {
                ChatId = chatId,
                UserId = userId,
                Title = title
            });
        }

        public Task UserConnected(string userId)
        {
            return SendAsync("userConnected", new UserIdentifierNotification
            {
                UserId = userId
            });
        }

        public Task UserDisconnected(string userId)
        {
            return SendAsync("userDisconnected", new UserIdentifierNotification
            {
                UserId = userId
            });
        }
    }
}

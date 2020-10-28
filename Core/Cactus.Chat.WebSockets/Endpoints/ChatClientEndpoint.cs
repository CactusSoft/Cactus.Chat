using System;
using System.Text;
using System.Threading.Tasks;
using Cactus.Chat.Logging;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Transport;
using Cactus.Chat.Transport.Models.Output;
using StreamJsonRpc;

namespace Cactus.Chat.WebSockets.Endpoints
{
    public class ChatClientEndpoint : IChatClientEndpoint
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ChatClientEndpoint));
        private readonly JsonRpc _rpc;

        public ChatClientEndpoint(JsonRpc rpc)
        {
            Log.Debug(".ctor");
            _rpc = rpc;
        }

        protected Task NotifyAsync(string message, params object[] arguments)
        {
            if (Log.IsDebugEnabled())
            {
                var sb = new StringBuilder("Notify ");
                sb.Append(message);
                if (arguments?.Length > 0)
                {
                    sb.Append('(');
                    for (var i = 0; i < 0; i++)
                    {
                        sb.Append(arguments[i]);
                        if (i < arguments.Length - 1)
                            sb.Append(", ");
                    }

                    sb.Append(')');
                }
                Log.Debug(sb.ToString());
            }
            return _rpc.NotifyAsync(message, arguments);
        }

        public Task MessageRead(string chatId, string userId, DateTime timestamp)
        {
            return NotifyAsync("messageRead", new MessageIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId,
                Timestamp = timestamp
            });
        }

        public Task MessageDelivered(string chatId, string userId, DateTime timestamp)
        {
            return NotifyAsync("messageDelivered", new MessageIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId,
                Timestamp = timestamp
            });
        }

        public Task MessageNew<T>(string chatId, T message) where T : InstantMessage
        {
            return NotifyAsync("messageNew", new MessageNewNotification<T>
            {
                ChatId = chatId,
                Message = message
            });
        }

        public Task ParticipantAdded<T>(string chatId, string userId, string participantId, T profile) where T : IChatProfile
        {
            return NotifyAsync("participantAdded", new ParticipantAddedNotification<T>
            {
                ChatId = chatId,
                UserId = userId,
                Participant = new NotificationParticipant<T>
                {
                    Id = participantId,
                    Profile = profile
                }
            });
        }

        public Task ParticipantLeft(string chatId, string userId)
        {
            return NotifyAsync("participantLeft", new ParticipantIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId
            });
        }

        public Task ParticipantStartTyping(string chatId, string userId)
        {
            return NotifyAsync("participantStartTyping", new ParticipantIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId
            });
        }

        public Task ParticipantStopTyping(string chatId, string userId)
        {
            return NotifyAsync("participantStopTyping", new ParticipantIdentifierNotification
            {
                ChatId = chatId,
                UserId = userId
            });
        }

        public Task ChatTitleChanged(string chatId, string userId, string title)
        {
            return NotifyAsync("chatTitleChanged", new ChatTitleChangedNotification
            {
                ChatId = chatId,
                UserId = userId,
                Title = title
            });
        }

        public Task UserConnected(string userId)
        {
            return NotifyAsync("userConnected", new UserIdentifierNotification
            {
                UserId = userId
            });
        }

        public Task UserDisconnected(string userId)
        {
            return NotifyAsync("userDisconnected", new UserIdentifierNotification
            {
                UserId = userId
            });
        }
    }
}

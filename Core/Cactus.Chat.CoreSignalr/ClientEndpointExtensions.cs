using System.Threading.Tasks;
using Cactus.Chat.Events;
using Cactus.Chat.Transport;

namespace Cactus.Chat.Signalr
{
    public static class ClientEndpointExtensions
    {
        public static Task MessageRead(this IChatClientEndpoint client, IMessageIdentifier mid)
        {
            return client.MessageRead(mid.ChatId, mid.UserId, mid.Timestamp);
        }

        public static Task MessageDelivered(this IChatClientEndpoint client, IMessageIdentifier mid)
        {
            return client.MessageDelivered(mid.ChatId, mid.UserId, mid.Timestamp);
        }

        public static Task ParticipantLeft(this IChatClientEndpoint client, IParticipantIdentifier mid)
        {
            return client.ParticipantLeft(mid.ChatId, mid.UserId);
        }

        public static Task ParticipantStartTyping(this IChatClientEndpoint client, IParticipantIdentifier mid)
        {
            return client.ParticipantStartTyping(mid.ChatId, mid.UserId);
        }

        public static Task ParticipantStopTyping(this IChatClientEndpoint client, IParticipantIdentifier mid)
        {
            return client.ParticipantStopTyping(mid.ChatId, mid.UserId);
        }
    }
}

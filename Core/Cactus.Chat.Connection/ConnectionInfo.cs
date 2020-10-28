using Cactus.Chat.Transport;

namespace Cactus.Chat.Connection
{
    class ConnectionInfo : IConnectionInfo
    {
        public ConnectionInfo(string id, string userId, string broadcastGroup, IChatClientEndpoint client)
        {
            Id = id;
            UserId = userId;
            BroadcastGroup = broadcastGroup;
            Client = client;
        }

        public string Id { get; }
        public string UserId { get; }
        public string BroadcastGroup { get; }
        public IChatClientEndpoint Client { get; }
    }
}
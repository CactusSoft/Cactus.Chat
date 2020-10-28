using System;
using Cactus.Chat.Connection;
using Cactus.Chat.Transport;

namespace Cactus.Chat.Signalr.Connections
{
    /// <summary>
    /// Immutable connection info bag. Hold it immutable.
    /// </summary>
    public class ConnectionInfo : IConnectionInfo
    {
        public ConnectionInfo(string connectionId, string userId, string broadcastGroup, IChatClientEndpoint client)
        {
            Id = connectionId ?? throw new ArgumentException("connectionId");
            UserId = userId ?? throw new ArgumentException("userId");
            BroadcastGroup = broadcastGroup;
            Client = client;
        }

        public string Id { get; }

        public string UserId { get; }

        public string BroadcastGroup { get; }

        public IChatClientEndpoint Client { get; }
    }
}

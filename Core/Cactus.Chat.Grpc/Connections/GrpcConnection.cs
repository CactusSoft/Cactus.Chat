using Cactus.Chat.Connection;
using Cactus.Chat.Transport;

namespace Cactus.Chat.Grpc.Connections
{
    public class GrpcConnection : IConnectionInfo
    {
        public GrpcConnection(string id, string userId, string broadcastGroup, IChatClientEndpoint client)
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
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Logging;
using Cactus.Chat.Transport;
using Cactus.Chat.WebSockets.Endpoints;
using StreamJsonRpc;

namespace Cactus.Chat.WebSockets.Connections
{
    /// <summary>
    /// Immutable connection info bag. Hold it immutable.
    /// </summary>
    public class ChatConnection : IChatConnection
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ChatConnection));
        private readonly IJrpcWebSocket _socket;
        protected JsonRpc JsonRpc;

        public ChatConnection(string connectionId, string userId, string broadcastGroup, WebSocket socket)
        {
            _socket = new JrpcWebSocket(socket);
            Id = connectionId ?? throw new ArgumentException("connectionId");
            UserId = userId ?? throw new ArgumentException("userId");
            BroadcastGroup = broadcastGroup;
            Client = new NullClientEndpoint();
        }

        public string Id { get; }

        public string UserId { get; }

        public string BroadcastGroup { get; }

        public IChatClientEndpoint Client { get; protected set; }

        public async Task ListenAsync(object target, CancellationToken cancellationToken)
        {
            if (JsonRpc == null)
            {
                Log.Debug("Init JsonRPC channel...");
                JsonRpc = new JsonRpc(_socket, target);
            }
            else
                throw new InvalidOperationException("JsonRPC has been already initiated. Calling StartListening twice is not an option.");

            Log.DebugFormat("Start listening {0}/{1}", Id, UserId);
            try
            {
                JsonRpc.StartListening();
                Client = new ChatClientEndpoint(JsonRpc);
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (await Task.WhenAny(JsonRpc.Completion, Task.Delay(3000, cancellationToken)) ==
                        JsonRpc.Completion)
                    {
                        //JsonRpc task has been completed, break the circle
                        break;
                    }
                }
            }
            finally
            {
                JsonRpc?.Dispose();
                Client = new NullClientEndpoint();
            }

            if (cancellationToken.IsCancellationRequested && !JsonRpc.Completion.IsCompleted)
            {
                //The listening has been cancelled outside. Do graceful shutdown?
                Log.DebugFormat("Listening stopped by outside cancellation request, {0}/{1}", Id, UserId);
            }
            else
            {
                Log.DebugFormat("Listening stopped by JsonRpc.Completion signal, {0}/{1}", Id, UserId);
                if (JsonRpc.Completion.Exception != null)
                    Log.WarnFormat("JsonRpc.Completion exception: {0}", JsonRpc.Completion.Exception);
            }

            JsonRpc = null;
        }
        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}

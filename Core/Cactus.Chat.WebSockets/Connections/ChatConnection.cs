using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Transport;
using Cactus.Chat.WebSockets.Endpoints;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Cactus.Chat.WebSockets.Connections
{
    /// <summary>
    /// Immutable connection info bag. Hold it immutable.
    /// </summary>
    public class ChatConnection : IChatConnection
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ChatConnection> _log;
        private readonly IJrpcWebSocket _socket;
        protected JsonRpc JsonRpc;

        public ChatConnection(string connectionId, string userId, string broadcastGroup, WebSocket socket, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _log = loggerFactory.CreateLogger<ChatConnection>();
            _socket = new JrpcWebSocket(socket,loggerFactory.CreateLogger<JrpcWebSocket>());
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
                _log.LogDebug("Init JsonRPC channel...");
                JsonRpc = new JsonRpc(_socket, target);
            }
            else
                throw new InvalidOperationException("JsonRPC has been already initiated. Calling StartListening twice is not an option.");

            _log.LogDebug("Start listening {connection_id}/{user_id}", Id, UserId);
            try
            {
                JsonRpc.StartListening();
                Client = new ChatClientEndpoint(JsonRpc,_loggerFactory.CreateLogger<ChatClientEndpoint>());
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
                _log.LogDebug("Listening stopped by outside cancellation request, {connection_id}/{user_id}", Id, UserId);
            }
            else
            {
                _log.LogDebug("Listening stopped by JsonRpc.Completion signal, {connection_id}/{user_id}", Id, UserId);
                if (JsonRpc.Completion.Exception != null)
                    _log.LogWarning("JsonRpc.Completion exception: {exception}", JsonRpc.Completion.Exception);
            }

            JsonRpc = null;
        }
        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}

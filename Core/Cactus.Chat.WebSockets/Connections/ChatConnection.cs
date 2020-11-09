using System;
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
        private readonly object _messageTarget;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ChatConnection> _log;
        private readonly IJrpcWebSocket _socket;
        private JsonRpc _jsonRpc;

        public ChatConnection(
            string connectionId,
            string userId,
            string broadcastGroup,
            JrpcWebSocket socket,
            object messageTarget,
            ILoggerFactory loggerFactory)
        {
            Id = connectionId ?? throw new ArgumentException(nameof(connectionId));
            UserId = userId ?? throw new ArgumentException(nameof(userId));
            _messageTarget = messageTarget;
            _loggerFactory = loggerFactory;
            _log = loggerFactory.CreateLogger<ChatConnection>();
            _socket = socket;
            BroadcastGroup = broadcastGroup;
            Client = new NullClientEndpoint();
        }

        public string Id { get; }

        public string UserId { get; }

        public string BroadcastGroup { get; }

        public IChatClientEndpoint Client { get; protected set; }

        public async Task ListenAsync(CancellationToken cancellationToken = default)
        {
            if (_jsonRpc == null)
            {
                _log.LogDebug("Init JsonRPC channel...");
                _jsonRpc = new JsonRpc(_socket, _messageTarget);
            }
            else
                throw new InvalidOperationException(
                    "JsonRPC has been already initiated. Calling StartListening twice is not an option.");

            _log.LogDebug("Start listening {connection_id}/{user_id}", Id, UserId);
            try
            {
                _jsonRpc.StartListening();
                Client = new ChatClientEndpoint(_jsonRpc, _loggerFactory.CreateLogger<ChatClientEndpoint>());
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (await Task.WhenAny(_jsonRpc.Completion, Task.Delay(3000, cancellationToken)) ==
                        _jsonRpc.Completion)
                    {
                        //JsonRpc task has been completed, break the circle
                        break;
                    }
                }
            }
            finally
            {
                _jsonRpc?.Dispose();
                Client = new NullClientEndpoint();
            }

            if (cancellationToken.IsCancellationRequested && !_jsonRpc.Completion.IsCompleted)
            {
                //The listening has been cancelled outside. Do graceful shutdown?
                _log.LogDebug("Listening stopped by outside cancellation request, {connection_id}/{user_id}", Id,
                    UserId);
            }
            else
            {
                _log.LogDebug("Listening stopped by JsonRpc.Completion signal, {connection_id}/{user_id}", Id, UserId);
                if (_jsonRpc.Completion.Exception != null)
                    _log.LogWarning("JsonRpc.Completion exception: {exception}", _jsonRpc.Completion.Exception);
            }

            _jsonRpc = null;
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}
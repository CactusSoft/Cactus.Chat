using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Connection;
using Cactus.Chat.External;
using Cactus.Chat.Grpc.Connections;
using Cactus.Chat.Grpc.Endpoints;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cactus.Chat.Grpc
{
    public class ChatCallbackService : ChatCallback.ChatCallbackBase
    {
        private readonly IConnectionStorage _connectionStorage;
        private readonly IEventHub _eventHub;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ChatCallbackService> _log;

        public ChatCallbackService(IConnectionStorage connectionStorage, IEventHub eventHub,
            ILoggerFactory loggerFactory)
        {
            _connectionStorage = connectionStorage;
            _eventHub = eventHub;
            _loggerFactory = loggerFactory;
            _log = loggerFactory.CreateLogger<ChatCallbackService>();
        }

        public override async Task Connect(
            ConnectParams request,
            IServerStreamWriter<Notification> responseStream,
            ServerCallContext context)
        {
            _log.LogDebug("Income connection from {host}", context.Host);
            if (!context.AuthContext.IsPeerAuthenticated)
            {
                await SendError(responseStream, 401, "Not authenticated");
                return;
            }

            _log.LogDebug("Trying to get user_id...");
            var userId = context.AuthContext.PeerIdentity
                .FirstOrDefault(e => e.Name == context.AuthContext.PeerIdentityPropertyName)
                ?.Value;

            if (userId == null)
            {
                await SendError(responseStream, 401, "Not authenticated, unable to get user id");
                return;
            }

            var client = new ChatClientEndpoint(responseStream, _loggerFactory.CreateLogger<ChatClientEndpoint>());
            var connection = new GrpcConnection(context.GetHttpContext().Connection.Id, userId, "*", client);
            _connectionStorage.Add(connection);
            await _eventHub.FireEvent(new UserConnected {UserId = userId});
            _log.LogDebug("Connection added, start awaiting");
            await AwaitCancellation(context.CancellationToken);
            _connectionStorage.Delete(connection.Id);
            await _eventHub.FireEvent(new UserDisconnected {UserId = userId});
            _log.LogDebug("Connection {connection_id}/{user_id} finished", connection.Id, connection.UserId);
        }

        private async Task SendError(IServerStreamWriter<Notification> responseStream, int code, string message)
        {
            _log.LogWarning("Not authenticated, return 401 error");
            await responseStream.WriteAsync(new Notification
            {
                Error = new Error
                {
                    Code = code,
                    Message = message
                }
            });
        }

        private static Task AwaitCancellation(CancellationToken token)
        {
            var completion = new TaskCompletionSource<object>();
            token.Register(() => completion.SetResult(null));
            return completion.Task;
        }
    }
}
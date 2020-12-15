using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Connection;
using Cactus.Chat.Events;
using Cactus.Chat.External;
using Cactus.Chat.WebSockets.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cactus.Chat.WebSockets
{
    public abstract class AcceptConnectionAbstractMiddleware
    {
        private readonly ILogger _log;

        public AcceptConnectionAbstractMiddleware(ILogger log)
        {
            _log = log;
            _log.LogDebug(".ctor");
        }

        protected async Task InvokeAsync(HttpContext ctx, IEventHub eventHub, IConnectionStorage connectionStorage)
        {
            _log.LogDebug("Someone knocked to WS/JRPC endpoint...");
            if (!ctx.User.Identity.IsAuthenticated)
            {
                _log.LogWarning("Unauthenticated request, return HTTP 401");
                ctx.Response.StatusCode = 401;
                return;
            }

            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                _log.LogWarning("Not a WebSocket request, return HTTP 400");
                ctx.Response.StatusCode = 400;
                return;
            }

            var auth = BuildAuthContext(ctx);
            var userId = auth.GetUserId();
            var broadcastGroup = GetBroadcastGroup(auth);
            _log.LogDebug("Income connection: {connection_id}/{user_id}:{broadcast_group}",
                auth.ConnectionId, userId, broadcastGroup);
            // var chatService = b.ApplicationServices
            //     .GetRequiredService<IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>();

            var socket = await ctx.WebSockets.AcceptWebSocketAsync();
            using var chatConnection = BuildChatConnection(auth, broadcastGroup, socket);
            connectionStorage.Add(chatConnection);
            try
            {
                var listenTask = chatConnection.ListenAsync(CancellationToken.None);

                _log.LogDebug("Connected: {connection_id}/{user_id}, send UserConnected broadcast",
                    auth.ConnectionId, userId);
#pragma warning disable 4014
                //DO NOT await it
                eventHub.FireEvent(new UserConnected
                {
                    BroadcastGroup = broadcastGroup,
                    ConnectionId = auth.ConnectionId,
                    UserId = auth.GetUserId()
                });
#pragma warning restore 4014

                _log.LogDebug("Start awaiting: {connection_id}/{user_id}", auth.ConnectionId, userId);
                await listenTask;
            }
            finally
            {
                connectionStorage.Delete(auth.ConnectionId);
                _log.LogDebug("Awaiting finished: {connection_id}/{user_id}, send UserDisconnect broadcast",
                    auth.ConnectionId, userId);
                await eventHub.FireEvent(new UserDisconnected
                {
                    BroadcastGroup = broadcastGroup,
                    ConnectionId = auth.ConnectionId,
                    UserId = auth.GetUserId()
                });
            }
        }

        protected abstract IAuthContext BuildAuthContext(HttpContext ctx);

        protected abstract string GetBroadcastGroup(IAuthContext auth);

        protected abstract IChatConnection BuildChatConnection(IAuthContext auth, string broadcastGroup,
            WebSocket socket);
    }
}
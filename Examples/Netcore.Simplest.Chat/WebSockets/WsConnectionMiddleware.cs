using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.WebSockets;
using Cactus.Chat.WebSockets.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Netcore.Simplest.Chat.Integration;
using Netcore.Simplest.Chat.Models;


namespace Netcore.Simplest.Chat.WebSockets
{
    public class WsConnectionMiddleware : AcceptConnectionAbstractMiddleware
    {
        private readonly IConnectionStorage _connectionStorage;
        private readonly IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> _chatService;
        private readonly ILoggerFactory _loggerFactory;

        public WsConnectionMiddleware(RequestDelegate next,
            IConnectionStorage connectionStorage,
            IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> chatService,
            ILoggerFactory loggerFactory
        ) : base(loggerFactory.CreateLogger<WsConnectionMiddleware>())
        {
            _connectionStorage = connectionStorage;
            _chatService = chatService;
            _loggerFactory = loggerFactory;
        }

        public Task InvokeAsync(HttpContext ctx, IEventHub eventHub)
        {
            return InvokeAsync(ctx, eventHub, _connectionStorage);
        }

        protected override IAuthContext BuildAuthContext(HttpContext ctx)
        {
            return new AuthContext(ctx.User.Identity)
            {
                ConnectionId = Guid.NewGuid().ToString("N")
            };
        }

        protected override string GetBroadcastGroup(IAuthContext auth)
        {
            var broadcastGroup = "*";
            var userId = auth.GetUserId();
            var broadcastDelimiterIndex = userId.IndexOf('@');
            if (broadcastDelimiterIndex > 0 && broadcastDelimiterIndex < userId.Length)
                broadcastGroup = userId.Substring(broadcastDelimiterIndex + 1);
            return broadcastGroup;
        }

        protected override IChatConnection BuildChatConnection(IAuthContext auth, string broadcastGroup,
            WebSocket socket)
        {
            var jrpcWebSocket = new JrpcWebSocket(socket, _loggerFactory.CreateLogger<JrpcWebSocket>());
            var soketTargetService = new JrpcChatServerEndpoint(_chatService, auth, _connectionStorage,
                _loggerFactory.CreateLogger<JrpcChatServerEndpoint>());

            return new ChatConnection(
                auth.ConnectionId,
                auth.GetUserId(),
                broadcastGroup,
                jrpcWebSocket,
                soketTargetService,
                TimeSpan.FromSeconds(6),
                _loggerFactory
            );
        }
    }
}
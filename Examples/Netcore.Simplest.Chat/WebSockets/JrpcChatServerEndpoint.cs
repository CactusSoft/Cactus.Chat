using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.WebSockets;
using Cactus.Chat.WebSockets.Endpoints;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.WebSockets
{
    public class JrpcChatServerEndpoint : ChatServerEndpoint<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>
    {
        public JrpcChatServerEndpoint(IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> chatService, IAuthContext auth, IConnectionStorage connectionStorage)
        :base(chatService, auth, connectionStorage)
        {

        }
    }
}

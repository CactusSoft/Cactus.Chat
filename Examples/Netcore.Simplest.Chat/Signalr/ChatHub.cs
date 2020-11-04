using System.Security.Principal;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Signalr;
using log4net;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Netcore.Simplest.Chat.Integration;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.Signalr
{
    public class ChatHub : AbstractChatHub<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile, ChatHub>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ChatHub));

        public ChatHub(IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> chatService,
            IConnectionStorage connectionStorage, IEventHub bus, IHubContext<Hub> hubContext,
            IHubContext<ChatHub> hubContext2, ILogger<ChatHub> log)
            : base(chatService, connectionStorage, bus, log)
        {
            HubContext = hubContext2;
        }

        protected override IAuthContext AuthContext => new AuthContext(Context.User.Identity, Context.ConnectionId);
        protected override IHubContext<ChatHub> HubContext { get; }

        protected override string GetBroadcastGroup(IIdentity identity)
        {
            var @index = identity.Name?.IndexOf('@') ?? -1;
            if (@index < 0 || @index >= identity.Name?.Length - 1)
            {
                Log.InfoFormat("No broadcast group defined for {0}", Context.User.Identity.Name);
                return null; //No broadcast group. Nobody will receive notification about the user connect/disconnect 
            }

            // The same broadcast group for everyone. This way everybody gets connected/disconnected evenys of others
            return identity.Name.Substring(@index + 1);
        }
    }
}
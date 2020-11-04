using Cactus.Chat.Autofac;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.Events;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Signalr;
using Microsoft.Extensions.Logging;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.Signalr
{
    class ChatEventHandler : AbstractEventHandler<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>,
        IEventHandler<MessageRead>,
        IEventHandler<MessageDelivered>,
        IEventHandler<MessageNew<CustomIm>>,
        IEventHandler<ParticipantAdded<CustomProfile>>,
        IEventHandler<ParticipantLeftChat>,
        IEventHandler<ParticipantStartTyping>,
        IEventHandler<ParticipantStopTyping>,
        IEventHandler<ChatTitleUpdated>,
        IEventHandler<UserConnected>,
        IEventHandler<UserDisconnected>
    {
        public ChatEventHandler(IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> chatService,
            IEventHub bus, IConnectionStorage connectionStorage, ILogger<ChatEventHandler> log)
            : base(chatService, bus, connectionStorage, log)
        {
        }
    }
}
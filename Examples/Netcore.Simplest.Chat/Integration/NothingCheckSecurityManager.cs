using System;
using System.Threading.Tasks;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using log4net;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.Integration
{
    public class NothingCheckSecurityManager : ISecurityManager<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (NothingCheckSecurityManager));
        public Task TryStart(IAuthContext me, Chat<CustomIm, CustomProfile> chat)
        {
            Log.Warn("TryStart didn't check");
            return Task.CompletedTask;
        }

        public Task TryRead(IAuthContext me, string chatId, Lazy<Chat<CustomIm, CustomProfile>> chat)
        {
            Log.Warn("TryRead didn't check");
            return Task.CompletedTask;
        }
        
        public Task TrySendMessage(IAuthContext me, string chatId, CustomIm msg, Lazy<Chat<CustomIm, CustomProfile>> chat)
        {
            Log.Warn("TrySendMessage didn't check");
            return Task.CompletedTask;
        }

        public Task TryAddParticipant(IAuthContext me, string chatId, string participantId, Lazy<Chat<CustomIm, CustomProfile>> chat)
        {
            Log.Warn("TryAddParticipant didn't check");
            return Task.CompletedTask;
        }
    }
}
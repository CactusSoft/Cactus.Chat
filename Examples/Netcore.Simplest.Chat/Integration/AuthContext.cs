using System.Security.Principal;
using System.Threading;
using Cactus.Chat.External;
using Cactus.Chat.Signalr;
using log4net;
using Microsoft.AspNetCore.SignalR;

namespace Netcore.Simplest.Chat.Integration
{
    public class AuthContext : IAuthContext, IUserIdProvider
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AuthContext));

        public AuthContext()
        {
            Log.Debug("Default .ctor");
        }

        public AuthContext(IIdentity identity)
        {
            Log.Debug("Identity .ctor");
            Identity = identity;
        }

        public AuthContext(IIdentity identity, string connectionId) : this(identity)
        {
            Log.Debug("Connection .ctor");
            ConnectionId = connectionId;
        }

        public object Identity { get; }

        public string ConnectionId { get; set; }

        public string GetUserId()
        {
            return getUserId(Identity as IIdentity ?? Thread.CurrentPrincipal.Identity);
        }

        public string GetUserId(HubConnectionContext connection)
        {
            var res = getUserId(connection.User.Identity);
            Log.DebugFormat("Gettinng userId from SignalR request: {0}", res);
            return res;
        }

        private string getUserId(IIdentity identity)
        {
            var uid = identity.Name;
            if (uid == null)
                throw new ErrorInfo("Unauthenticated", 123);
            return uid;
        }
    }
}
using System;
using System.Net.WebSockets;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Connection;

namespace Cactus.Chat.WebSockets.Connections
{
    public interface IChatConnection : IConnectionInfo, IDisposable
    {
        Task ListenAsync(object target, CancellationToken cancellationToken);
    }

    public interface IChatConnectionFactory
    {
        IChatConnection Create(WebSocket socket, IIdentity identity);
    }
   
}

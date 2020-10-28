//using System;
//using System.IO;
//using System.Net.WebSockets;
//using System.Threading;
//using System.Threading.Tasks;
//using Cactus.Chat.Logging;
//using Cactus.Chat.WebSockets.Connections;
//using Microsoft.AspNetCore.Http;

//namespace Cactus.Chat.WebSockets
//{
//    public class AcceptConnectionMiddleware : IMiddleware
//    {
//        private static readonly ILog Log = LogProvider.GetLogger(typeof(AcceptConnectionMiddleware));
//        private readonly IConnectionStorage _connectionStorage;
//        private readonly IIdentityInfoProvider _identityInfoProvider;
//        protected static readonly int ReadBufferSize = 1024 * 4;

//        public AcceptConnectionMiddleware(IConnectionStorage connectionStorage, IIdentityInfoProvider identityInfoProvider)
//        {
//            Log.Debug(".ctor");
//            _connectionStorage = connectionStorage;
//            _identityInfoProvider = identityInfoProvider;
//        }

//        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
//        {
//            if (context.WebSockets.IsWebSocketRequest)
//            {
//                var userId = _identityInfoProvider.GetUserId(context.User.Identity);
//                if (!string.IsNullOrEmpty(userId))
//                {
//                    var broadcastGroup = _identityInfoProvider.GetBroadcastGroup(context.User.Identity);
//                    var socket = await context.WebSockets.AcceptWebSocketAsync();
//                    var connectionInfo = new ConnectionInfo(socket, userId, broadcastGroup);
//                    _connectionStorage.Add(connectionInfo);
//                    Log.InfoFormat("User {0} connected", userId);
//                    await Listen(connectionInfo);
//                }
//                else
//                {
//                    Log.Warn("Unable to extract User ID from identity, return HTTP 401");
//                    context.Response.StatusCode = 401;
//                }
//            }
//        }
//            else
//            {
//                await next(context);
//    }
//}

//private async Task Listen(ConnectionInfo connection)
//{
//    var buffer = new byte[ReadBufferSize];
//    var socket = connection.Socket;
//    var res = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//    while (res.MessageType != WebSocketMessageType.Close)
//    {
//        if (res.MessageType == WebSocketMessageType.Text)
//        {
//            using (var payloadStream = await ReceivePayloadAsync(connection, res, buffer))
//            {
//                JsonRpc.Attach(socket.UsePipe().AsStream(),);
//                //TODO: processing
//            }
//        }
//        else
//        {
//            Log.ErrorFormat("Unexpected data type received: {0}. Forward the package to dev/null", res.MessageType.ToString("G"));
//            var size = res.Count;
//            while (!res.EndOfMessage)
//            {
//                res = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//                size += res.Count;
//            }
//            Log.InfoFormat("{0} bytes sent to dev/null", size);
//        }
//    }

//    Log.Info("Connection {0}/{1} closed, status: {2} / {3}",
//        connection.ConnectionId,
//        connection.UserId,
//        socket.CloseStatus.HasValue ? socket.CloseStatus.Value.ToString("G") : "unknown",
//        socket.CloseStatusDescription);
//    _connectionStorage.Delete(connection.ConnectionId);
//}

//private async Task<Stream> ReceivePayloadAsync(ConnectionInfo connection, WebSocketReceiveResult res, byte[] buffer)
//{
//    if (res.EndOfMessage)
//    {
//        return new MemoryStream(buffer, 0, res.Count);
//    }

//    var ms = new MemoryStream(buffer, 0, res.Count);
//    while (!res.EndOfMessage)
//    {
//        res = await connection.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
//        ms.Write(buffer, 0, res.Count);
//    }

//    ms.Flush();
//    ms.Seek(0, SeekOrigin.Begin);
//    return ms;
//}
//    }
//}

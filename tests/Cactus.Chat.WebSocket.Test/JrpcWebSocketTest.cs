using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.WebSockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StreamJsonRpc.Protocol;

namespace Cactus.Chat.WebSocket.Test
{
    [TestClass]
    public class JrpcWebSocketTest
    {
        [TestMethod]
        public async Task ReadTextTestAsync()
        {
            var wsMock = new Mock<System.Net.WebSockets.WebSocket>();
            wsMock.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())).Returns(
                (ArraySegment<byte> a, CancellationToken t) =>
                {
                    var encoding = new UTF8Encoding(false);
                    var output = encoding.GetBytes("{\"jsonrpc\": \"2.0\", \"method\": \"ping\", \"id\": \"1\"}");
                    output.CopyTo(a.AsSpan());
                    return Task.FromResult(new WebSocketReceiveResult(output.Length, WebSocketMessageType.Text, true));
                });

            var socket = new JrpcWebSocket(wsMock.Object, new NullLogger<JrpcWebSocket>());
            var res = await socket.ReadAsync(CancellationToken.None);
            Assert.IsNotNull(res);
            Assert.IsTrue(res is JsonRpcRequest);
            Assert.AreEqual("1", ((JsonRpcRequest) res).Id);
            wsMock.VerifyAll();
        }

        [TestMethod]
        public async Task ReadBinaryTestAsync()
        {
            var wsMock = new Mock<System.Net.WebSockets.WebSocket>();
            wsMock.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())).Returns(
                (ArraySegment<byte> a, CancellationToken t) =>
                {
                    var encoding = new UTF8Encoding(false);
                    var output = encoding.GetBytes("{\"jsonrpc\": \"2.0\", \"method\": \"ping\", \"id\": \"1\"}");
                    output.CopyTo(a.AsSpan());
                    return Task.FromResult(new WebSocketReceiveResult(output.Length, WebSocketMessageType.Binary,
                        true));
                });

            var socket = new JrpcWebSocket(wsMock.Object, new NullLogger<JrpcWebSocket>());
            var res = await socket.ReadAsync(CancellationToken.None);
            Assert.IsNotNull(res);
            Assert.IsTrue(res is JsonRpcRequest);
            Assert.AreEqual("1", ((JsonRpcRequest) res).Id);
            wsMock.VerifyAll();
        }

        [TestMethod]
        public async Task ReadCloseMessageTestAsync()
        {
            var socketCloseStatus = WebSocketCloseStatus.NormalClosure;
            var wsMock = new Mock<System.Net.WebSockets.WebSocket>();
            wsMock.Setup(ws => ws.CloseStatus).Returns(socketCloseStatus);
            wsMock.Setup(ws => ws.CloseStatusDescription).Returns(JrpcWebSocket.GoodbyMessage);
            wsMock.Setup(ws => ws.CloseOutputAsync(
                    It.Is<WebSocketCloseStatus>(v => v == socketCloseStatus),
                    It.Is<string>(v => v == JrpcWebSocket.GoodbyMessage),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            wsMock.Setup(ws => ws.ReceiveAsync(
                    It.IsAny<ArraySegment<byte>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((ArraySegment<byte> a, CancellationToken t) =>
                    Task.FromResult(new WebSocketReceiveResult(0, WebSocketMessageType.Close, true)));

            var socket = new JrpcWebSocket(wsMock.Object, new NullLogger<JrpcWebSocket>());
            Assert.IsNull(await socket.ReadAsync(CancellationToken.None));
            wsMock.VerifyAll();
        }

        [TestMethod]
        public async Task ReadTwoPiecesTestAsync()
        {
            var pos = 0;
            var oneReadCapacity = 40; //Read by blocks of 40 bytes
            var message =
                new UTF8Encoding(false).GetBytes("{\"jsonrpc\": \"2.0\", \"method\": \"ping\", \"id\": \"2\"}");
            var wsMock = new Mock<System.Net.WebSockets.WebSocket>();
            wsMock.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())).Returns(
                (ArraySegment<byte> a, CancellationToken t) =>
                {
                    var output = message.AsSpan(pos, Math.Min(oneReadCapacity, message.Length - pos));
                    output.CopyTo(a.AsSpan());
                    pos += output.Length;
                    return Task.FromResult(new WebSocketReceiveResult(output.Length, WebSocketMessageType.Text,
                        pos >= message.Length));
                });

            var socket = new JrpcWebSocket(wsMock.Object, new NullLogger<JrpcWebSocket>());
            var res = await socket.ReadAsync(CancellationToken.None);

            Assert.IsNotNull(res);
            Assert.IsTrue(res is JsonRpcRequest);
            Assert.AreEqual("2", ((JsonRpcRequest) res).Id);
        }

        [TestMethod]
        public async Task ReadManyPiecesTestAsync()
        {
            var pos = 0;
            var oneReadCapacity = 4; //Read by blocks of 4 bytes
            var message =
                new UTF8Encoding(false).GetBytes(
                    "{\"jsonrpc\": \"2.0\", \"method\": \"ReadManyPiecesTest\", \"id\": \"3\"}");
            var wsMock = new Mock<System.Net.WebSockets.WebSocket>();
            wsMock.Setup(ws => ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())).Returns(
                (ArraySegment<byte> a, CancellationToken t) =>
                {
                    var output = message.AsSpan(pos, Math.Min(oneReadCapacity, message.Length - pos));
                    output.CopyTo(a.AsSpan());
                    pos += output.Length;
                    return Task.FromResult(new WebSocketReceiveResult(output.Length, WebSocketMessageType.Text,
                        pos >= message.Length));
                });

            var socket = new JrpcWebSocket(wsMock.Object, new NullLogger<JrpcWebSocket>());
            var res = await socket.ReadAsync(CancellationToken.None);

            Assert.IsNotNull(res);
            Assert.IsTrue(res is JsonRpcRequest);
            Assert.AreEqual("3", ((JsonRpcRequest) res).Id);
            Assert.AreEqual("ReadManyPiecesTest", ((JsonRpcRequest) res).Method);
        }

        [TestMethod]
        public async Task WriteTestAsync()
        {
            var request = new JsonRpcRequest
            {
                Id = 3,
                Method = nameof(WriteTestAsync)
            };
            var wsMock = new Mock<System.Net.WebSockets.WebSocket>();

            wsMock.Setup(ws => ws.SendAsync(
                    It.Is<ArraySegment<byte>>(v => v.Count > 0),
                    It.Is<WebSocketMessageType>(v => v == WebSocketMessageType.Text),
                    It.Is<bool>(v => v),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var socket = new JrpcWebSocket(wsMock.Object, new NullLogger<JrpcWebSocket>());
            await socket.WriteAsync(request, CancellationToken.None);
            wsMock.VerifyAll();
        }
    }
}
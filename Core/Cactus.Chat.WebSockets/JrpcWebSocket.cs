using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Logging;
using Nerdbank.Streams;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;
[assembly: InternalsVisibleTo("Cactus.Chat.WebSocket.Test")]

namespace Cactus.Chat.WebSockets
{
    public interface IJrpcWebSocket : IJsonRpcMessageHandler, IDisposable
    {
        Task ShutdownAsync(string reason);
        WebSocketState State { get; }
    }

    public class JrpcWebSocket : IJrpcWebSocket
    {
        internal static readonly string GoodbyMessage = "Goodby, see you!";
        protected static readonly int ReadBufferSize = 1024 * 4;
        private static readonly ILog Log = LogProvider.GetLogger(typeof(JrpcWebSocket));
        private readonly WebSocket _ws;
        private readonly IJsonRpcMessageTextFormatter _serializer;
        private bool _disposed;
        private readonly SemaphoreSlim _shutdownLock = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1);

        public JrpcWebSocket(WebSocket ws)
        {
            _ws = ws;
            _serializer = new JsonMessageFormatter();
        }

        public async ValueTask<JsonRpcMessage> ReadAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException("websocket");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var buffer = new byte[ReadBufferSize];

                var res = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (res.MessageType != WebSocketMessageType.Close)
                {
                    var payload = await ReceivePayloadAsync(res, buffer, cancellationToken);
                    return _serializer.Deserialize(payload);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info("We are shutting down...");
                if (_ws.State == WebSocketState.Open)
                {
                    Log.Info("Saying goodby...");
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Shutting down", CancellationToken.None);
                }
                throw;
            }

            Log.Info("Close message received, do graceful socket shutdown...");
            var closeStatus = _ws.CloseStatus.HasValue ? _ws.CloseStatus.Value.ToString("G") : "unknown";
            await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, GoodbyMessage, cancellationToken);
            Log.Info("WebSocket closed, status: {0} / {1}", closeStatus, _ws.CloseStatusDescription);
            return null;
        }

        public bool CanRead => _ws.State == WebSocketState.Open && !_disposed;
        public bool CanWrite => _ws.State == WebSocketState.Open && !_disposed;
        public IJsonRpcMessageFormatter Formatter => _serializer;

        public async Task ShutdownAsync(string reason)
        {
            if (State == WebSocketState.Open || State == WebSocketState.CloseReceived)
            {
                if (await _shutdownLock.WaitAsync(200))
                {
                    if (_ws.State == WebSocketState.Open || State == WebSocketState.CloseReceived)
                    {
                        await _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
                    }
                    else
                    {
                        Log.Info("Websocket is {0}, nothing to shutdown.", _ws.State.ToString("G"));
                    }

                    _shutdownLock.Release();
                }
                else
                {
                    Log.WarnFormat("Waiting for shutdown semaphore is failed. Do nothing.");
                }
            }
            else
            {
                Log.WarnFormat("Websocket is {0}, no way to shutdown. Do nothing.", _ws.State.ToString("G"));
            }
        }

        public async ValueTask WriteAsync(JsonRpcMessage message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_disposed)
                throw new ObjectDisposedException("websocket");

            if (!await _writeLock.WaitAsync(5000, cancellationToken))
                throw new SynchronizationLockException("Unable to retrieve lock for write operation");

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using (var builder = new Sequence<byte>())
                {
                    _serializer.Serialize(builder, message);
                    var output = builder.AsReadOnlySequence.ToArray();
                    await _ws.SendAsync(output, WebSocketMessageType.Text, true, cancellationToken);
                    Log.Debug(() => $"Sent: {Encoding.UTF8.GetString(output)}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on write to socket");
                throw;
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public WebSocketState State => _ws.State;

        private async Task<ReadOnlySequence<byte>> ReceivePayloadAsync(WebSocketReceiveResult res, byte[] buffer, CancellationToken cancellationToken)
        {
            if (res.EndOfMessage)
            {
                var msg = new ReadOnlySequence<byte>(buffer, 0, res.Count);
                Log.Debug(() => $"Received at once: {Encoding.UTF8.GetString(msg.ToArray())}");
                return msg;
            }

            cancellationToken.ThrowIfCancellationRequested();
            using var ms = new MemoryStream(buffer.Length * 2);
            {
                await ms.WriteAsync(buffer, 0, res.Count, cancellationToken);
                while (!res.EndOfMessage)
                {
                    res = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    await ms.WriteAsync(buffer, 0, res.Count, cancellationToken);
                }

                await ms.FlushAsync(cancellationToken);
                ms.Seek(0, SeekOrigin.Begin);
                var msg = new ReadOnlySequence<byte>(ms.ToArray());
                Log.Debug(() => $"Received through mem stream: {Encoding.UTF8.GetString(msg.ToArray())}");
                return msg;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _ws?.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }
}
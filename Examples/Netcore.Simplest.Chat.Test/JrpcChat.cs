using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Transport.Models.Input;
using Cactus.Chat.Transport.Models.Output;
using Cactus.Chat.WebSockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netcore.Simplest.Chat.Models;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Netcore.Simplest.Chat.Test
{
    internal class JrpcChat : IDisposable
    {
        private readonly JrpcWebSocket _jrpcSocket;
        private readonly StackChatClientEndpoint _clientEndpoint;
        private readonly JsonRpc _rpc;
        public JrpcWebSocket Socket => _jrpcSocket;
        public ConcurrentStack<ChatEvent> EventStack => _clientEndpoint.EventStack;

        public Auth Auth { get; protected set; }

        protected JrpcChat(Auth auth, JrpcWebSocket jrpcSocket, StackChatClientEndpoint clientEndpoint,
            JsonRpc rpcGateway)
        {
            Auth = auth;
            _jrpcSocket = jrpcSocket;
            _clientEndpoint = clientEndpoint;
            _rpc = rpcGateway;
        }

        public static async Task<JrpcChat> Connect(Uri uri, Auth auth)
        {
            var wsEndpoint = new Uri(uri + "?access_token=" + Uri.EscapeDataString(auth.BuildAuthToken()));
            var wsClient = new ClientWebSocket();
            await wsClient.ConnectAsync(wsEndpoint, CancellationToken.None);
            var target = new StackChatClientEndpoint();
            var jrpcWs = new JrpcWebSocket(wsClient, new NullLogger<JrpcWebSocket>());
            var rpc = new JsonRpc(jrpcWs, target);
            rpc.StartListening();
            rpc.TraceSource.Switch.Level = System.Diagnostics.SourceLevels.All;
            return new JrpcChat(auth, jrpcWs, target, rpc);
        }

        public async Task<Ping> Ping()
        {
            return await _rpc.InvokeAsync<Ping>("Ping");
        }

        public async Task Error()
        {
            await _rpc.InvokeAsync<Ping>("Error");
        }

        public void Dispose()
        {
            _jrpcSocket?.Dispose();
            _rpc?.Dispose();
        }

        public async Task<ChatSummary<CustomIm, CustomProfile>> StartChat(string title = null,
            params string[] participants)
        {
            Assert.IsNotNull(participants);
            Assert.AreNotEqual(0, participants.Length);
            return await _rpc.InvokeAsync<ChatSummary<CustomIm, CustomProfile>>("StartChat",
                new Chat<CustomIm, CustomProfile>
                {
                    Title = title,
                    Participants = participants.Select(e => new ChatParticipant<CustomProfile> {Id = e}).ToList()
                }
            );
        }

        public async Task StatTyping(string chatId)
        {
            await _rpc.InvokeAsync<JObject>("StartTyping", chatId);
        }

        public async Task Alive()
        {
            await _rpc.NotifyAsync("Alive");
        }

        public async Task StopTyping(string chatId)
        {
            await _rpc.InvokeAsync<JObject>("StopTyping", chatId);
        }

        public async Task<DateTime> SendMessage(string chatId, string message,
            IDictionary<string, object> metadata = null)
        {
            Assert.IsNotNull(chatId);
            Assert.IsNotNull(message);

            return await _rpc.InvokeAsync<DateTime>("SendMessage", chatId, new InstantMessage
            {
                Message = message,
                Metadata = metadata
            });
        }

        public async Task<IEnumerable<InstantMessage>> GetMessages(string chatId, DateTime? from = null,
            DateTime? to = null, int count = -1, bool moveBackward = false)
        {
            Assert.IsNotNull(chatId);
            return await _rpc.InvokeAsync<IEnumerable<InstantMessage>>("GetMessages", chatId, from, to, count,
                moveBackward);
        }

        public async Task AddParticipants(string chatId, params string[] users)
        {
            Assert.IsNotNull(chatId);
            Assert.IsNotNull(users);
            Assert.AreNotEqual(0, users.Length);
            await _rpc.InvokeAsync("AddParticipants", chatId, new AddParticipantsCommand
            {
                Ids = users
            });
        }

        public async Task<ChatSummary<CustomIm, CustomProfile>> GetChat(string chatId)
        {
            Assert.IsNotNull(chatId);
            return await _rpc.InvokeAsync<ChatSummary<CustomIm, CustomProfile>>("GetChat", chatId);
        }
        
        public Task<IEnumerable<UserStatus>> GetUserStatus(params string[] userIds)
        {
            return _rpc.InvokeAsync<IEnumerable<UserStatus>>("GetUserStatus", userIds?.ToList());
        }

        public async Task LeaveChat(string chatId)
        {
            Assert.IsNotNull(chatId);
            await _rpc.InvokeAsync("LeaveChat", chatId);
        }

        public Task GoodbyAsync()
        {
            return Socket.ShutdownAsync(Auth.User + " says goodby");
        }

        public async Task<List<ChatSummary<CustomIm, CustomProfile>>> GetChats()
        {
            return await _rpc.InvokeAsync<List<ChatSummary<CustomIm, CustomProfile>>>("GetChats");
        }

        public async Task<IEnumerable<string>> GetContactsOnline()
        {
            return await _rpc.InvokeAsync<IEnumerable<string>>("GetContactsOnline");
        }
    }
}
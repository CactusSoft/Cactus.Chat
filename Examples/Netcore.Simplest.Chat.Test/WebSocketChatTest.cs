using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Netcore.Simplest.Chat.Test
{
    [TestClass]
    public class WebSocketChatTest
    {
        protected static string BaseUrl = "http://docker10:806/";
        protected static string WebSocketPath = "/ws";
        private static IDisposable _hostedChat;

        [ClassInitialize]
        public static void StartSimpleChat(TestContext context)
        {
            BaseUrl = "http://localhost:5000/";
            BaseUrl = "http://localhost:5002/";

            if (_hostedChat == null)
                _hostedChat = WebHost.CreateDefaultBuilder()
                    .UseUrls(BaseUrl)
                    .UseStartup<Startup>()
                    .Start(BaseUrl);
        }

        [ClassCleanup]
        public static void ShutdownChat()
        {
            _hostedChat?.Dispose();
        }

        protected Uri WsEndpoint => new Uri(new Uri("ws://" + new Uri(BaseUrl).Host + ":" + new Uri(BaseUrl).Port),
            WebSocketPath);

        [TestMethod]
        public async Task PingTestAsync()
        {
            using (var chat = await JrpcChat.Connect(WsEndpoint, Auth.Timmy()))
            {
                var res = await chat.Ping();
                await chat.GoodbyAsync();
                Assert.IsTrue((DateTime.UtcNow - res.Timestamp).Seconds < 3);
            }
        }

        [TestMethod]
        public async Task GetContactsOnlineTestAsync()
        {
            using (var chat = await JrpcChat.Connect(WsEndpoint, Auth.Timmy()))
            using (await JrpcChat.Connect(WsEndpoint, Auth.Timmy()))
            using (await JrpcChat.Connect(WsEndpoint, Auth.Butters()))
            using (await JrpcChat.Connect(WsEndpoint, Auth.Butters()))
            using (await JrpcChat.Connect(WsEndpoint, Auth.Kartman()))
            using (await JrpcChat.Connect(WsEndpoint, Auth.Stranger()))
            {
                var res = (await chat.GetContactsOnline()).ToList();
                await chat.GoodbyAsync();
                Assert.AreEqual(2, res.Count);
                Assert.IsTrue(res.Any(e => e == Auth.Butters().User));
                Assert.IsTrue(res.Any(e => e == Auth.Kartman().User));
                Assert.IsFalse(res.Any(e => e == Auth.Stranger().User), "Stranger is in different BroadcastGroup");
            }
        }

        [TestMethod]
        public async Task ErrorTestAsync()
        {
            using (var chat = await JrpcChat.Connect(WsEndpoint, Auth.Timmy()))
            {
                try
                {
                    await chat.Error();
                }
                catch (Exception)
                {
                    Assert.AreEqual(WebSocketState.Open, chat.Socket.State);
                }

                try
                {
                    await chat.Error();
                }
                catch (Exception)
                {
                    Assert.AreEqual(WebSocketState.Open, chat.Socket.State);
                }
            }
        }

        [TestMethod]
        public async Task TimmyAuthTestAsync()
        {
            using (var timmy = await JrpcChat.Connect(WsEndpoint, Auth.Timmy()))
            using (var kenny = await JrpcChat.Connect(WsEndpoint, Auth.Kenny()))
            {
                var timmyPing = timmy.Ping();
                var kennyPing = kenny.Ping();
                await Task.WhenAll(timmyPing, kennyPing);

                Assert.AreEqual(timmy.Auth.User, (await timmyPing).UserId);
                Assert.AreEqual(kenny.Auth.User, (await kennyPing).UserId);
            }
        }

        [TestMethod]
        public async Task P2PChatTestAsync()
        {
            var salt = Guid.NewGuid().ToString("N");

            using (var timmy = await JrpcChat.Connect(WsEndpoint, Auth.Timmy(salt)))
            {
                await timmy.Ping();
                using (var butters = await JrpcChat.Connect(WsEndpoint, Auth.Butters(salt)))
                {
                    await Task.Delay(100);
                    var chatId = (await butters.StartChat(null, timmy.Auth.User)).Id;
                    var chat = await butters.GetChat(chatId);
                    Assert.AreEqual(2, chat.Participants.Count);
                    Assert.IsTrue(chat.Participants.First(p => p.Id == timmy.Auth.User).IsOnline);
                    await butters.StatTyping(chatId);
                    await Task.Delay(300);
                    await butters.StopTyping(chatId);
                    await Task.Delay(300);
                    await butters.SendMessage(chatId, "tru-la-la");
                    await Task.Delay(300);
                    var messages = (await butters.GetMessages(chatId, DateTime.MinValue, DateTime.MaxValue)).ToList();
                    Assert.IsNotNull(messages);
                    Assert.IsTrue(messages.Any());
                    Assert.AreEqual("tru-la-la", messages.Last().Message);
                    await Task.Delay(200);
                    await butters.GoodbyAsync();
                }
                // Wait chat is created and messages are sent
                await Task.Delay(500);
                await timmy.GoodbyAsync();
                ValidateEvents(timmy,
                    "userDisconnected",
                    "messageNew",
                    "participantStopTyping",
                    "participantStartTyping",
                    "userConnected");
            }
        }

        [TestMethod]
        public async Task ChatWithMyselfTestAsync()
        {
            var salt = Guid.NewGuid().ToString("N");
            using (var timmy1 = await JrpcChat.Connect(WsEndpoint, Auth.Timmy(salt)))
            {
                await timmy1.Ping();
                using (var timmy2 = await JrpcChat.Connect(WsEndpoint, Auth.Timmy(salt)))
                {
                    await Task.Delay(100);
                    var chatId = (await timmy2.StartChat(null, timmy1.Auth.User)).Id;
                    await timmy2.StatTyping(chatId);
                    await Task.Delay(300);
                    await timmy2.StopTyping(chatId);
                    await Task.Delay(300);
                    await timmy2.SendMessage(chatId, "tru-la-la");
                    var messages = (await timmy2.GetMessages(chatId, DateTime.MinValue, DateTime.MaxValue)).ToList();
                    Assert.IsNotNull(messages);
                    Assert.IsTrue(messages.Any());
                    Assert.AreEqual("tru-la-la", messages.Last().Message);
                    await Task.Delay(200);
                    await timmy2.GoodbyAsync();
                }

                // Wait chat is created and messages are sent
                await Task.Delay(200);
                await timmy1.GoodbyAsync();
                ValidateEvents(timmy1,
                    "messageNew",
                    "participantStopTyping",
                    "participantStartTyping");
            }
        }

        [TestMethod]
        public async Task P2PChatStartTwiceAsync()
        {
            var salt = Guid.NewGuid().ToString("N");
            var buttersAuth = Auth.Butters(salt);
            var timmyAuth = Auth.Timmy(salt);
            using (var timmy = await JrpcChat.Connect(WsEndpoint, timmyAuth))
            {
                var chatId1 = (await timmy.StartChat(null, buttersAuth.User)).Id;
                using (var butters = await JrpcChat.Connect(WsEndpoint, buttersAuth))
                {
                    var chatId2 = (await butters.StartChat(null, timmyAuth.User)).Id;
                    await Task.Delay(200);
                    Assert.IsNotNull(chatId1);
                    Assert.IsNotNull(chatId2);
                    Assert.AreEqual(chatId1, chatId2);
                }
            }
        }

        [TestMethod]
        public async Task MultiChatAndP2pChatAsync()
        {
            var salt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(salt);
            var buttersAuth = Auth.Butters(salt);
            var kartmanAuth = Auth.Kartman(salt);
            var stanAuth = Auth.Stan(salt);

            string groupChatId, p2pChatId;

            using (var timmy = await JrpcChat.Connect(WsEndpoint, timmyAuth))
            {
                groupChatId = (await timmy.StartChat(null, buttersAuth.User, kartmanAuth.User)).Id;
                await timmy.AddParticipants(groupChatId, stanAuth.User);
                var chat = await timmy.GetChat(groupChatId);
                Assert.AreEqual(4, chat.Participants.Count);
                await timmy.SendMessage(groupChatId, "group chat msg");

                using (var stan = await JrpcChat.Connect(WsEndpoint, stanAuth))
                {
                    await stan.LeaveChat(groupChatId);
                    using (var butters = await JrpcChat.Connect(WsEndpoint, buttersAuth))
                    {
                        p2pChatId = (await butters.StartChat(null, timmyAuth.User)).Id;
                        await butters.SendMessage(p2pChatId, "p2p chat msg");
                        await butters.SendMessage(groupChatId, "p2p chat msg");
                        await Task.Delay(300);
                        await butters.Socket.ShutdownAsync("Butters says goodby");
                    }
                    await stan.Socket.ShutdownAsync("Stan says goodby");
                }
                await timmy.Socket.ShutdownAsync("Timmy says goodby");
            }
            Assert.IsNotNull(groupChatId);
            Assert.IsNotNull(p2pChatId);
            Assert.AreNotEqual(groupChatId, p2pChatId);
        }

        [TestMethod]
        public async Task Start2p2pChatSimuteniouslyAsync()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            string chatId1 = null, chatId2 = null;
            ConcurrentStack<ChatEvent> timmyEvents, buttersEvents;
            using (var timmy = await JrpcChat.Connect(WsEndpoint, timmyAuth))
            using (var butters = await JrpcChat.Connect(WsEndpoint, buttersAuth))
            {
                var timmyTask = Task.Run(async () =>
                {
                    chatId1 = (await timmy.StartChat(null, buttersAuth.User)).Id;
                    await timmy.SendMessage(chatId1, "Timmyyyyy!!!");
                });

                var buttersTask = Task.Run(async () =>
                {
                    chatId2 = (await butters.StartChat(null, timmyAuth.User)).Id;
                    await butters.SendMessage(chatId2, "Hello Timmy!");
                });

                await Task.WhenAll(timmyTask, buttersTask);
                // Wait chat is created and messages are sent
                await Task.Delay(500);
                timmyEvents = timmy.EventStack;
                buttersEvents = butters.EventStack;
            }

            Assert.AreEqual(chatId2, chatId1);
            Trace.WriteLine("Buttest events (" + buttersEvents.Count + "):");
            foreach (var ev in buttersEvents)
            {
                Trace.WriteLine(ev.Method);
            }
            Trace.WriteLine("Timmy events (" + timmyEvents.Count + "):");
            foreach (var ev in timmyEvents)
            {
                Trace.WriteLine(ev.Method);
            }
            Assert.IsNotNull(buttersEvents.Single(e => e.Method == "messageNew" && e.ChatId == chatId1), "Ooops, Butters did not received message");
            Assert.IsNotNull(timmyEvents.Single(e => e.Method == "messageNew" && e.ChatId == chatId1), "Ooops, Timmy did not received message");
        }

        [TestMethod]
        public async Task ConnectRushAsync()
        {
            var rnd = new Random();
            ServicePointManager.DefaultConnectionLimit = 500;
            var connections = new Task<JrpcChat>[20]; //20 is for dummy CI server, 200 is OK for good machine
            for (var i = 0; i < connections.Length; i++)
            {
                connections[i] = Task.Run(async () =>
                {
                    var auth = new Auth("timmy" + rnd.Next(0, connections.Length) + "@connect.rush");
                    var x = await JrpcChat.Connect(WsEndpoint, auth);
                    await x.Ping();
                    return x;
                });
            }

            await Task.WhenAll(connections);
            foreach (var connection in connections)
            {
                if (!connection.IsFaulted && !connection.IsCanceled && connection.IsCompleted)
                {
                    await connection.Result.GoodbyAsync();
                    connection.Result.Dispose();
                }
            }

            foreach (var connection in connections)
            {
                Assert.IsFalse(connection.IsFaulted);
            }
        }

        [TestMethod]
        public async Task P2PChatLeaveAndRestartTestAsync()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            using (var timmy = await JrpcChat.Connect(WsEndpoint, timmyAuth))
            {
                await timmy.Ping(); //force to connect before butters
                using (var butters = await JrpcChat.Connect(WsEndpoint, buttersAuth))
                {
                    var chatId = (await butters.StartChat(null, timmyAuth.User)).Id;
                    await butters.SendMessage(chatId, "I'm going to leave the chat");
                    await butters.LeaveChat(chatId);
                    var chatList = await butters.GetChats();
                    Assert.IsFalse(chatList.Any(e => e.Id == chatId));
                    var restartedChat = await butters.StartChat(null, timmyAuth.User);
                    Assert.AreEqual(chatId, restartedChat.Id);
                    chatList = await butters.GetChats();
                    Assert.IsTrue(chatList.Any(e => e.Id == chatId));
                    await butters.GoodbyAsync();
                }
                await Task.Delay(200);
                ValidateEvents(timmy,
                    "userDisconnected",
                    "participantLeft",
                    "messageNew",
                    "userConnected"
                    );
            }
        }

        [TestMethod]
        public async Task P2PChatLeaveAndReviveTestAsync()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            using (var timmy = await JrpcChat.Connect(WsEndpoint, timmyAuth))
            {
                await timmy.Ping(); //force to connect before butters
                using (var butters = await JrpcChat.Connect(WsEndpoint, buttersAuth))
                {
                    var chatId = (await butters.StartChat(null, timmyAuth.User)).Id;
                    await butters.SendMessage(chatId, "I'm going to leave the chat");
                    await butters.LeaveChat(chatId);
                    var chatList = await butters.GetChats();
                    Assert.IsFalse(chatList.Any(e => e.Id == chatId));
                    await timmy.SendMessage(chatId, "Get back!");
                    chatList = await butters.GetChats();
                    Assert.IsTrue(chatList.Any(e => e.Id == chatId));

                    await butters.GoodbyAsync();
                }

                await Task.Delay(200);
                await timmy.GoodbyAsync();
                ValidateEvents(timmy,
                    "userDisconnected",
                    "participantLeft",
                    "messageNew",
                    "userConnected");
            }
        }

        [TestMethod]
        public async Task P2PChatLeftUserReceiveEventsTestAsync()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            using (var timmy = await JrpcChat.Connect(WsEndpoint, timmyAuth))
            {
                await timmy.StartChat(null, buttersAuth.User);
                using (var butters = await JrpcChat.Connect(WsEndpoint, buttersAuth))
                {
                    var chatId = (await butters.StartChat(null, timmyAuth.User)).Id;
                    await butters.SendMessage(chatId, "Go away from the chat");
                    await Task.Delay(200);
                    await timmy.LeaveChat(chatId);
                    await butters.StatTyping(chatId);
                    await butters.SendMessage(chatId, "Are you here?");
                    await butters.StopTyping(chatId);

                    await butters.GoodbyAsync();
                }
                await Task.Delay(200);
                ValidateEvents(timmy,
                    "userDisconnected",
                    "participantStopTyping",
                    "messageNew",
                    "participantStartTyping",
                    "messageNew",
                    "userConnected"
                    );
            }
        }

        [TestMethod]
        public async Task P2PChatBothUserLeavesAndReviveTestAsync()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            using (var timmy = await JrpcChat.Connect(WsEndpoint, timmyAuth))
            {
                await timmy.StartChat(null, buttersAuth.User);
                using (var butters = await JrpcChat.Connect(WsEndpoint, buttersAuth))
                {
                    var chatId = (await butters.StartChat(null, timmyAuth.User)).Id;
                    await butters.SendMessage(chatId, "Go away from the chat");
                    await Task.Delay(200);
                    await timmy.LeaveChat(chatId);
                    await butters.LeaveChat(chatId);
                    await butters.StatTyping(chatId);
                    await butters.SendMessage(chatId, "Are you here?");
                    await butters.StopTyping(chatId);
                }

                await Task.Delay(200);
                ValidateEvents(timmy,
                    "userDisconnected",
                    "participantStopTyping",
                    "messageNew",
                    "participantStartTyping",
                    "participantLeft",
                    "messageNew",
                    "userConnected"
                    );
            }
        }

        private void ValidateEvents(JrpcChat chat, params string[] methods)
        {
            Assert.IsNotNull(chat);
            Assert.IsNotNull(chat.EventStack);
            Assert.IsNotNull(methods);
            var i = 0;
            while (i < methods.Length && chat.EventStack.TryPop(out var e))
            {
                Assert.AreEqual(methods[i], e.Method, "fail on event #" + i);
                i++;
            }
            Assert.AreEqual(methods.Length, i, "Mismatch event count");
        }
    }
}

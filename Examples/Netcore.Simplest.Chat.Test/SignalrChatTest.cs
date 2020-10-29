using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netcore.Simplest.Chat.Test
{
    [TestClass]
    public class SignalrChatTest
    {
        protected static string BaseUrl = "http://localhost:5552/";
        protected static string SignalrPath = "/sr";
        private static IDisposable _hostedChat;

        [ClassInitialize]
        public static void StartSimpleChat(TestContext context)
        {
            BaseUrl = "http://localhost:5000/";
            Environment.SetEnvironmentVariable("ASPNETCORE_URLS", BaseUrl);

            if (_hostedChat == null)
                _hostedChat = WebHost.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(c =>
                        c.AddInMemoryCollection(new[] {new KeyValuePair<string, string>("storage:type", "inMemory"),}))
                    .UseKestrel()
                    .UseUrls(BaseUrl)
                    .UseStartup<Startup>()
                    .Start(BaseUrl);
            //Trace.Listeners.Clear();
            //Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [ClassCleanup]
        public static void ShutdownChat()
        {
            _hostedChat?.Dispose();
        }

        [TestMethod]
        public async Task BadIdentityCauseToFail()
        {
            var signalrEndpoint = new Uri(new Uri(BaseUrl), SignalrPath);
            var connection = new HubConnectionBuilder()
                .WithUrl(signalrEndpoint, o => o.Headers.Add("Authorization", "bullshit"))
                .ConfigureLogging(b => b.AddConsole().AddDebug().SetMinimumLevel(LogLevel.Trace))
                .Build();

            await connection.StartAsync();
            try
            {
                await connection.Ping();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is HubException, $"Not a HubException, but {ex.GetType()}: {ex.Message}");
                Assert.IsTrue(ex.Message.Contains("ErrorInfo: {"),
                    $"Error info not found in the message: {ex.Message}");
                return;
            }
            finally
            {
                await connection.DisposeAsync();
            }

            Assert.Fail("No exception was risen");
        }

        [TestMethod]
        public async Task Ping()
        {
            var timmy = await Connect(Auth.Timmy(), TraceCallback);
            await timmy.Ping();
            await timmy.StopAsync();
            await timmy.DisposeAsync();
        }

        [TestMethod]
        public async Task TimmyAuthTest()
        {
            var timmy = await Connect(Auth.Timmy(), TraceCallback);
            var butters = await Connect(Auth.Butters(), TraceCallback);
            var timmyPingRes = await timmy.Ping();
            var buttersPingRes = await butters.Ping();

            await timmy.StopAsync();
            await timmy.DisposeAsync();
            await butters.StopAsync();
            await butters.DisposeAsync();

            Assert.AreEqual("timmy@south.park", timmyPingRes["UserId"].Value<string>());
            Assert.AreEqual("butters@south.park", buttersPingRes["UserId"].Value<string>());
        }

        [TestMethod]
        public async Task P2PChatTest()
        {
            var solt = Guid.NewGuid().ToString("N");

            var timmyAuth = Auth.Timmy(solt);
            var timmy = await Connect(timmyAuth);
            await timmy.hub.Ping();

            var butters = await Connect(Auth.Butters(solt));
            await Task.Delay(100);
            var chatId = (await butters.hub.StartChat(null, timmyAuth.User)).Id;
            await butters.hub.StatTyping(chatId);
            await Task.Delay(300);
            await butters.hub.StopTyping(chatId);
            await Task.Delay(300);
            await butters.hub.SendMessage(chatId, "tru-la-la");
            await Task.Delay(300);
            var messages = (await butters.hub.GetMessages(chatId, DateTime.MinValue, DateTime.MaxValue)).ToList();
            Assert.IsNotNull(messages);
            Assert.IsTrue(messages.Any());
            Assert.AreEqual("tru-la-la", messages.Last().Message);
            await Task.Delay(200);
            await butters.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await Task.Delay(200);

            // Wait chat is created and messages are sent
            await Task.Delay(500);
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();
            Assert.AreEqual("userDisconnected", timmy.events.Pop().Key);
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("participantStopTyping", timmy.events.Pop().Key);
            Assert.AreEqual("participantStartTyping", timmy.events.Pop().Key);
            Assert.AreEqual("userConnected", timmy.events.Pop().Key);
        }

        [TestMethod]
        public async Task ChatWithMyselfTest()
        {
            var timmy1Auth = Auth.Timmy();
            var timmy = await Connect(timmy1Auth);
            await timmy.hub.Ping();

            var timmy2 = await Connect(Auth.Timmy());
            await Task.Delay(100);
            var chatId = (await timmy2.hub.StartChat(null, timmy1Auth.User)).Id;
            await timmy2.hub.StatTyping(chatId);
            await Task.Delay(300);
            await timmy2.hub.StopTyping(chatId);
            await Task.Delay(300);
            await timmy2.hub.SendMessage(chatId, "tru-la-la");
            var messages = (await timmy2.hub.GetMessages(chatId, DateTime.MinValue, DateTime.MaxValue)).ToList();
            Assert.IsNotNull(messages);
            Assert.IsTrue(messages.Any());
            Assert.AreEqual("tru-la-la", messages.Last().Message);
            await Task.Delay(200);
            await timmy2.hub.StopAsync();
            await timmy2.hub.DisposeAsync();

            // Wait chat is created and messages are sent
            await Task.Delay(200);
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("participantStopTyping", timmy.events.Pop().Key);
            Assert.AreEqual("participantStartTyping", timmy.events.Pop().Key);
        }

        [TestMethod]
        public async Task P2PChatStartTwice()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);
            var timmy = await Connect(timmyAuth);
            var chatId1 = (await timmy.hub.StartChat(null, buttersAuth.User)).Id;

            var butters = await Connect(buttersAuth);
            var chatId2 = (await butters.hub.StartChat(null, timmyAuth.User)).Id;

            await Task.Delay(200);
            await butters.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();

            Assert.IsNotNull(chatId1);
            Assert.IsNotNull(chatId2);
            Assert.AreEqual(chatId1, chatId2);
        }

        [TestMethod]
        public async Task MultiChatAndP2pChat()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);
            var kartmanAuth = Auth.Kartman(solt);
            var stanAuth = Auth.Stan(solt);

            var timmy = await Connect(timmyAuth);
            var groupChatId = (await timmy.hub.StartChat(null, buttersAuth.User, kartmanAuth.User)).Id;
            await timmy.hub.AddParticipants(groupChatId, stanAuth.User);
            var chat = await timmy.hub.GetChat(groupChatId);
            Assert.AreEqual(4, chat["Participants"].Values<JToken>().Count());
            await timmy.hub.SendMessage(groupChatId, "group chat msg");

            var stan = await Connect(stanAuth);
            await stan.hub.LeaveChat(groupChatId);

            var butters = await Connect(buttersAuth);
            var p2pChatId = (await butters.hub.StartChat(null, timmyAuth.User)).Id;
            await butters.hub.SendMessage(p2pChatId, "p2p chat msg");
            await butters.hub.SendMessage(groupChatId, "p2p chat msg");

            // Wait chat is created and messages are sent
            await Task.Delay(300);
            await butters.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();
            await stan.hub.StopAsync();
            await stan.hub.DisposeAsync();

            Assert.IsNotNull(groupChatId);
            Assert.IsNotNull(p2pChatId);
            Assert.AreNotEqual(groupChatId, p2pChatId);
        }

        [TestMethod]
        public async Task Start2p2pChatSimuteniously()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            string chatId1 = null, chatId2 = null;
            var timmy = await Connect(timmyAuth);
            var butters = await Connect(buttersAuth);

            var timmyTask = Task.Run(async () =>
            {
                chatId1 = (await timmy.hub.StartChat(null, buttersAuth.User)).Id;
                await timmy.hub.SendMessage(chatId1, "Timmyyyyy!!!");
            });

            var buttersTask = Task.Run(async () =>
            {
                chatId2 = (await butters.hub.StartChat(null, timmyAuth.User)).Id;
                await butters.hub.SendMessage(chatId2, "Hello Timmy!");
            });

            await Task.WhenAll(timmyTask, buttersTask);
            // Wait chat is created and messages are sent
            await Task.Delay(500);
            await butters.hub.StopAsync();
            await timmy.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await timmy.hub.DisposeAsync();

            Assert.AreEqual(chatId2, chatId1);
            Trace.WriteLine("Buttest events (" + butters.events.Count + "):");
            foreach (var ev in butters.events)
            {
                Trace.WriteLine(ev.Key + " : " + ev);
            }

            Trace.WriteLine("Timmy events (" + timmy.events.Count + "):");
            foreach (var ev in timmy.events)
            {
                Trace.WriteLine(ev.Key);
            }

            Assert.IsNotNull(
                butters.events.Single(e => e.Key == "messageNew" && e.Value["ChatId"].Value<string>() == chatId1),
                "Ooops, Butters did not received message");
            Assert.IsNotNull(
                timmy.events.Single(e => e.Key == "messageNew" && e.Value["ChatId"].Value<string>() == chatId1),
                "Ooops, Timmy did not received message");
        }

        [TestMethod]
        public async Task ConnectRush()
        {
            var rnd = new Random();
            var connections = new Task<HubConnection>[20];
            for (var i = 0; i < connections.Length; i++)
            {
                connections[i] = Task.Run(async () =>
                {
                    var x = await Connect(Auth.Timmy(rnd.Next(0, 20).ToString()));
                    await x.hub.Ping();
                    return x.hub;
                });
            }

            await Task.WhenAll(connections);
            foreach (var connection in connections)
            {
                if (!connection.IsFaulted && !connection.IsCanceled && connection.IsCompleted)
                    await connection.Result.DisposeAsync();
            }

            foreach (var connection in connections)
            {
                Assert.IsFalse(connection.IsFaulted);
            }
        }

        [TestMethod]
        public async Task P2PChatLeaveAndRestartTest()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            var timmy = await Connect(timmyAuth);
            await timmy.hub.Ping(); //force to connect before butters

            var butters = await Connect(buttersAuth);
            var chatId = (await butters.hub.StartChat(null, timmyAuth.User)).Id;
            await butters.hub.SendMessage(chatId, "I'm going to leave the chat");
            await butters.hub.LeaveChat(chatId);
            var chatList = await butters.hub.GetChats();
            Assert.IsFalse(chatList.Any(e => e.Id == chatId));
            var restartedChat = await butters.hub.StartChat(null, timmyAuth.User);
            Assert.AreEqual(chatId, restartedChat.Id);
            chatList = await butters.hub.GetChats();
            Assert.IsTrue(chatList.Any(e => e.Id == chatId));

            await butters.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await Task.Delay(200);
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();

            // Wait chat is created and messages are sent
            Assert.AreEqual("userDisconnected", timmy.events.Pop().Key);
            Assert.AreEqual("participantLeft", timmy.events.Pop().Key);
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("userConnected", timmy.events.Pop().Key);
        }

        [TestMethod]
        public async Task P2PChatLeaveAndReviveTest()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            var timmy = await Connect(timmyAuth);
            await timmy.hub.Ping(); //force to connect before butters

            var butters = await Connect(buttersAuth);
            var chatId = (await butters.hub.StartChat(null, timmyAuth.User)).Id;
            await butters.hub.SendMessage(chatId, "I'm going to leave the chat");
            await butters.hub.LeaveChat(chatId);
            var chatList = await butters.hub.GetChats();
            Assert.IsFalse(chatList.Any(e => e.Id == chatId));
            await timmy.hub.SendMessage(chatId, "Get back!");
            chatList = await butters.hub.GetChats();
            Assert.IsTrue(chatList.Any(e => e.Id == chatId));

            await butters.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await Task.Delay(200);
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();

            // Wait chat is created and messages are sent
            Assert.AreEqual("userDisconnected", timmy.events.Pop().Key);
            Assert.AreEqual("participantLeft", timmy.events.Pop().Key);
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("userConnected", timmy.events.Pop().Key);
        }

        [TestMethod]
        public async Task P2PChatLeftUserReceiveEventsTest()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            var timmy = await Connect(timmyAuth);
            await timmy.hub.StartChat(null, buttersAuth.User);

            var butters = await Connect(buttersAuth);
            var chatId = (await butters.hub.StartChat(null, timmyAuth.User)).Id;
            await butters.hub.SendMessage(chatId, "Go away from the chat");
            await Task.Delay(200);
            await timmy.hub.LeaveChat(chatId);
            await butters.hub.StatTyping(chatId);
            await butters.hub.SendMessage(chatId, "Are you here?");
            await butters.hub.StopTyping(chatId);

            await butters.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await Task.Delay(200);
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();

            // Wait chat is created and messages are sent
            Assert.AreEqual("userDisconnected", timmy.events.Pop().Key);
            Assert.AreEqual("participantStopTyping", timmy.events.Pop().Key);
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("participantStartTyping", timmy.events.Pop().Key);
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("userConnected", timmy.events.Pop().Key);
        }

        [TestMethod]
        public async Task P2PChatBothUserLeavesAndReviveTest()
        {
            var solt = Guid.NewGuid().ToString("N");
            var timmyAuth = Auth.Timmy(solt);
            var buttersAuth = Auth.Butters(solt);

            var timmy = await Connect(timmyAuth);
            await timmy.hub.StartChat(null, buttersAuth.User);

            var butters = await Connect(buttersAuth);
            var chatId = (await butters.hub.StartChat(null, timmyAuth.User)).Id;
            await butters.hub.SendMessage(chatId, "Go away from the chat");
            await Task.Delay(200);
            await timmy.hub.LeaveChat(chatId);
            await butters.hub.LeaveChat(chatId);
            await butters.hub.StatTyping(chatId);
            await butters.hub.SendMessage(chatId, "Are you here?");
            await butters.hub.StopTyping(chatId);

            await butters.hub.StopAsync();
            await butters.hub.DisposeAsync();
            await Task.Delay(200);
            await timmy.hub.StopAsync();
            await timmy.hub.DisposeAsync();

            // Wait chat is created and messages are sent
            Assert.AreEqual("userDisconnected", timmy.events.Pop().Key);
            Assert.AreEqual("participantStopTyping", timmy.events.Pop().Key);
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("participantStartTyping", timmy.events.Pop().Key);
            Assert.AreEqual("participantLeft", timmy.events.Pop().Key);
            Assert.AreEqual("messageNew", timmy.events.Pop().Key);
            Assert.AreEqual("userConnected", timmy.events.Pop().Key);
        }

        private void TraceCallback(string method, JObject payload)
        {
            Trace.WriteLine($"{method} : {payload}");
        }

        private void TraceCallback(string user, string method, JObject payload)
        {
            Trace.WriteLine($"{user} : {method} : {payload.ToString(Formatting.None)}");
        }

        private async Task<HubConnection> Connect(Auth auth, Action<string, JObject> callBackAction)
        {
            var signalrEndpoint = new Uri(new Uri(BaseUrl), SignalrPath);
            var connection = new HubConnectionBuilder()
                .WithUrl(signalrEndpoint, o => o.Headers.Add("Authorization", auth.BuildAuthToken()))
                .ConfigureLogging(b => b.AddConsole().AddDebug())
                .AddNewtonsoftJsonProtocol()
                .Build();

            connection.On<JObject>("messageNew", x => callBackAction("messageNew", x));
            connection.On<JObject>("messageRead", x => callBackAction("messageRead", x));
            connection.On<JObject>("messageDelivered", x => callBackAction("messageDelivered", x));
            connection.On<JObject>("userConnected", x => callBackAction("userConnected", x));
            connection.On<JObject>("userDisconnected", x => callBackAction("userDisconnected", x));
            connection.On<JObject>("participantStartTyping", x => callBackAction("participantStartTyping", x));
            connection.On<JObject>("participantStopTyping", x => callBackAction("participantStopTyping", x));
            connection.On<JObject>("participantAdded", x => callBackAction("participantAdded", x));
            connection.On<JObject>("participantLeft", x => callBackAction("participantLeft", x));

            await connection.StartAsync();
            TraceCallback("Connected", JObject.FromObject(new {auth.User}));
            connection.Closed += e =>
            {
                TraceCallback("Connection closed",
                    e == null
                        ? JObject.FromObject(new {auth.User})
                        : JObject.FromObject(new {auth.User, Exception = e}));
                return Task.CompletedTask;
            };

            return connection;
        }

        private async Task<(HubConnection hub, Stack<KeyValuePair<string, JObject>> events)> Connect(Auth auth)
        {
            var res2 = new Stack<KeyValuePair<string, JObject>>();
            var res1 = await Connect(auth, (method, payload) =>
            {
                TraceCallback(auth.UserPrettyName, method, payload);
                res2.Push(new KeyValuePair<string, JObject>(method, payload));
            });
            return (res1, res2);
        }
    }
}
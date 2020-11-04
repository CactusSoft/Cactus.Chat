using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cactus.Chat.Grpc;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netcore.Simplest.Chat.Test.Grpc;
using ConnectParams = Cactus.Chat.Grpc.ConnectParams;
using Notification = Netcore.Simplest.Chat.Test.Grpc.Notification;

namespace Netcore.Simplest.Chat.Test
{
    [TestClass]
    public class GrpcChatTest
    {
        protected static string BaseUrl = "http://docker10:806/";
        protected static string Path = "/grpc";
        private static IDisposable _hostedChat;

        [ClassInitialize]
        public static void StartSimpleChat(TestContext context)
        {
            BaseUrl = "http://localhost:5000/";


            if (_hostedChat == null)
                _hostedChat = WebHost.CreateDefaultBuilder()
                    .ConfigureAppConfiguration(c =>
                        c.AddInMemoryCollection(new[] {new KeyValuePair<string, string>("storage:type", "inMemory"),}))
                    .UseUrls(BaseUrl)
                    .UseStartup<Startup>()
                    .Start(BaseUrl);
        }

        [ClassCleanup]
        public static void ShutdownChat()
        {
            _hostedChat?.Dispose();
        }

        

        [TestMethod]
        public async Task ConnectAsync()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            
            using var channel = GrpcChannel.ForAddress(BaseUrl);
            var client = new ChatEventHub.ChatEventHubClient(channel);
            var headers = new Metadata {{"Authorization", Auth.Timmy().BuildAuthToken()}};

            using var stream=client.Connect(new Grpc.ConnectParams(), headers);
            var tokenSource = new CancellationTokenSource();
            await ConsumeEvents(stream, tokenSource.Token);
        }

        private async Task ConsumeEvents(AsyncServerStreamingCall<Notification> stream, CancellationToken ct)
        {
            try
            {
                await foreach (var msg in stream.ResponseStream.ReadAllAsync(ct))
                {
                    
                }
            }
            catch (RpcException e) when (e.StatusCode == StatusCode.Cancelled)
            {
                return;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Finished.");
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

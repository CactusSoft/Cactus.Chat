using System;
using System.Threading;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.Events;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.WebSockets.Connections;
using Cactus.TimmyAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Netcore.Simplest.Chat.Integration;
using Netcore.Simplest.Chat.Models;
using Netcore.Simplest.Chat.Signalr;
using Netcore.Simplest.Chat.Start;
using Netcore.Simplest.Chat.WebSockets;
using Newtonsoft.Json.Serialization;

namespace Netcore.Simplest.Chat
{
    public class Startup
    {
        private readonly IConfiguration _config;
        private readonly ILogger<Startup> _log;
        private IContainer _container;

        public Startup(IConfiguration config, ILoggerFactory loggerFactory)
        {
            _log = loggerFactory.CreateLogger<Startup>();
            _config = config;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            _log.LogInformation("ConfigureServices...");
            services
                .AddSignalR()
                .AddJsonProtocol(o =>
                {
//                    o.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver();
                })
                .AddHubOptions<ChatHub>(o =>
                {
                    o.EnableDetailedErrors = true;
                    o.HandshakeTimeout = TimeSpan.FromSeconds(5);
                });
            services.Configure<TimmyAuthOptions>(o => o.AuthQueryKey = "access_token");
            services.AddAuthentication(o => o.DefaultAuthenticateScheme = "TIMMY").AddTimmyAuth();
            var storageSettings = _config.GetSection("storage")?.Get<StorageSettings>() ?? new StorageSettings();
            var builder = new ContainerBuilder()
                .ConfigureDependencies(storageSettings)
                .ConfigureMessageBus()
                .ConfigureSignalR();
            builder.Populate(services);
            _container = builder.Build();
            return new AutofacServiceProvider(_container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IConfiguration config)
        {
            _log.LogInformation("Configure...");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app
                .UseAuthentication()
                .Use(async (context, func) =>
                {
                    await context.AuthenticateAsync();
                    await func();
                })
                .Map("/ws", b => b
                     .UseWebSockets()
                     .Run(async ctx =>
                    {
                        _log.LogDebug("Someone knocked to /ws endpoint...");
                        if (!ctx.User.Identity.IsAuthenticated)
                        {
                            _log.LogWarning("Unauthenticated request, return HTTP 401");
                            ctx.Response.StatusCode = 401;
                            return;
                        }
                        if (!ctx.WebSockets.IsWebSocketRequest)
                        {
                            _log.LogWarning("Not a WebSocket request, return HTTP 400");
                            ctx.Response.StatusCode = 400;
                            return;
                        }

                        var connectionId = Guid.NewGuid().ToString("N");
                        var auth = new AuthContext(ctx.User.Identity) { ConnectionId = connectionId };
                        var userId = auth.GetUserId();
                        _log.LogDebug("Income connection: {0}/{1}", connectionId, userId);
                        var eventHub = b.ApplicationServices.GetRequiredService<IEventHub>();
                        var connectionStorage = b.ApplicationServices.GetRequiredService<IConnectionStorage>();
                        var chatService = b.ApplicationServices.GetRequiredService<IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>();
                        var broadcastGroup = "*";
                        var broadcastDelimeterIndex = userId.IndexOf('@');
                        if (broadcastDelimeterIndex > 0 && broadcastDelimeterIndex < userId.Length)
                            broadcastGroup = userId.Substring(broadcastDelimeterIndex + 1);
                        _log.LogDebug("Broadcast group: {0}", broadcastGroup);
                        var socket = await ctx.WebSockets.AcceptWebSocketAsync();
                        using (var chatConnection = new ChatConnection(connectionId, auth.GetUserId(), broadcastGroup, socket))
                        {
                            //TODO broadcast userConnected/userDisconnected
                            connectionStorage.Add(chatConnection);
                            var listenTask = chatConnection.ListenAsync(new JrpcChatServerEndpoint(chatService, auth, connectionStorage),
                                CancellationToken.None);

                            _log.LogDebug("{0}/{1} connected, send UserConnected broadcast", connectionId, userId);

                            
#pragma warning disable 4014
                            //DO NOT await it
                            eventHub.FireEvent(new UserConnected
                            {
                                BroadcastGroup = broadcastGroup,
                                ConnectionId = connectionId,
                                UserId = auth.GetUserId()
                            });
#pragma warning restore 4014                            


                            await listenTask;
                            connectionStorage.Delete(connectionId);
                            _log.LogDebug("Connection {0}/{1} is closed, send UserDisconnected broadcast", connectionId, userId);
                            await eventHub.FireEvent(new UserDisconnected
                            {
                                BroadcastGroup = broadcastGroup,
                                ConnectionId = connectionId,
                                UserId = auth.GetUserId()
                            });
                        }
                    }))
                .Map("/sr", b => b
                    .UseSignalR(routes =>
                    {
                        routes.MapHub<ChatHub>("");
                    })
                ).Run(async ctx =>
                {
                    _log.LogWarning("Default handler reached. Wrong request?");
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "plain/text";
                    await ctx.Response.WriteAsync("JsonRPC over WebSockets? No. SignalR Core? Nope... hmm... What the damn are you looking for then???");
                });
        }
    }


}

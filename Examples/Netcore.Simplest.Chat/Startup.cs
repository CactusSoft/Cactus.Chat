using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Cactus.Chat.Grpc;
using Cactus.TimmyAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Netcore.Simplest.Chat.Signalr;
using Netcore.Simplest.Chat.Start;
using Netcore.Simplest.Chat.WebSockets;
using Newtonsoft.Json;
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
            services.AddGrpc();
            services.AddControllers();
            services
                .AddSignalR()
                .AddNewtonsoftJsonProtocol(o =>
                {
                    o.PayloadSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    o.PayloadSerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                    o.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver
                        {NamingStrategy = new DefaultNamingStrategy()};
                })
                // .AddJsonProtocol(o =>
                // {
                //     o.PayloadSerializerOptions.PropertyNamingPolicy=JsonNamingPolicy.CamelCase;
                // })
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
                    .UseMiddleware<WsConnectionMiddleware>()
                )
                .Map("/sr", b => b
                    .UseSignalR(routes => { routes.MapHub<ChatHub>(""); })
                )
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    //endpoints.MapControllers();
                    endpoints.MapGrpcService<ChatCallbackService>();
                })
                .Run(async ctx =>
                {
                    _log.LogWarning("Default handler reached. Wrong request?");
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = "plain/text";
                    await ctx.Response.WriteAsync(
                        "JsonRPC over WebSockets? No. SignalR Core? Nope... hmm... What the damn are you looking for then???");
                });
        }
    }
}
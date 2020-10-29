using System;
using System.Reflection;
using Autofac;
using Cactus.Chat.Autofac;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Storage;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using Netcore.Simplest.Chat.Integration;
using Netcore.Simplest.Chat.Models;
using Netcore.Simplest.Chat.Signalr;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Netcore.Simplest.Chat.Start
{
    public static class DiConfig
    {
        public static ContainerBuilder ConfigureDependencies(this ContainerBuilder builder, StorageSettings storageSettings)
        {
            if (storageSettings.Type == StorageSettings.StorageType.InMemory)
            {
                builder.RegisterType<InMemoryDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>()
                    .As<IChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>();
            }
            else if (storageSettings.Type == StorageSettings.StorageType.Mongo)
            {
                if (storageSettings.Mongo == null)
                    throw new ArgumentNullException(nameof(storageSettings.Mongo));
                if (storageSettings.Mongo.ConnectionString == null)
                    throw new ArgumentNullException(nameof(storageSettings.Mongo.ConnectionString));
                if (storageSettings.Mongo.DbName == null)
                    throw new ArgumentNullException(nameof(storageSettings.Mongo.DbName));

                builder.Register(c => new MongoClient(storageSettings.Mongo.ConnectionString));
                builder.Register(c => c.Resolve<MongoClient>().GetDatabase(storageSettings.Mongo.DbName));
                builder.Register(c => c.Resolve<IMongoDatabase>().GetCollection<Chat<CustomIm, CustomProfile>>("chats"));
                builder.RegisterType<MongoChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>()
                    .As<IChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>();
            }

            //Use default Chat & IM types and register service
            builder.RegisterType<ChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>()
               .As<IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>()
               .SingleInstance();

            //Now we need to care about external dependencies.
            builder.RegisterType<AuthContext>().As<IAuthContext>().InstancePerRequest();
            builder.RegisterType<BasicProfileProvider>().As<IUserProfileProvider<CustomProfile>>();
            builder.RegisterType<NothingCheckSecurityManager>().As<ISecurityManager<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>();
            builder.RegisterType<ChatHub>().As<Hub>();
            return builder;
        }

        public static ContainerBuilder ConfigureSignalR(this ContainerBuilder builder)
        {
            builder.RegisterHubs(Assembly.GetExecutingAssembly());
            builder.RegisterType<AuthContext>().As<IUserIdProvider>();
            builder.RegisterType<ConcurrentDictionaryConnectionStorage>().AsImplementedInterfaces().SingleInstance();

            builder.Register(b =>
            {
                var serializerSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                return JsonSerializer.Create(serializerSettings);
            }).As<JsonSerializer>().SingleInstance();

            return builder;
        }

        public static ContainerBuilder ConfigureWs(this ContainerBuilder builder)
        {
            

            return builder;
        }

        public static ContainerBuilder ConfigureMessageBus(this ContainerBuilder builder)
        {
            builder.RegisterType<AutofacEventHub>().AsImplementedInterfaces();
            builder.RegisterType<ChatEventHandler>().AsImplementedInterfaces();
            return builder;
        }
    }
}
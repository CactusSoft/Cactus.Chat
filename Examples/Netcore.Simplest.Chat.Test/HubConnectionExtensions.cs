using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netcore.Simplest.Chat.Models;
using Newtonsoft.Json.Linq;

namespace Netcore.Simplest.Chat.Test
{
    public static class HubConnectionExtensions
    {
        public static async Task<List<Chat<InstantMessage, CustomProfile>>> GetChats(this HubConnection hub)
        {
            Assert.IsNotNull(hub);
            return await hub.InvokeAsync<List<Chat<InstantMessage, CustomProfile>>>("GetChats");
        }

        public static Task<JObject> GetChat(this HubConnection hub, string chatId)
        {
            Assert.IsNotNull(hub);
            Assert.IsNotNull(chatId);
            return hub.InvokeAsync<JObject>("GetChat", chatId);
        }

        public static async Task<Chat<InstantMessage, CustomProfile>> StartChat(this HubConnection hub, string title = null, params string[] participants)
        {
            Assert.IsNotNull(participants);
            Assert.AreNotEqual(0, participants.Length);
            return await hub
                .InvokeAsync<Chat<InstantMessage, CustomProfile>>("StartChat", new Chat<InstantMessage, CustomProfile>
                {
                    Title = title,
                    Participants = participants.Select(e => new ChatParticipant<CustomProfile> { Id = e }).ToList()
                });
        }
        public static Task AddParticipants(this HubConnection hub, string chatId, params string[] users)
        {
            Assert.IsNotNull(hub);
            Assert.IsNotNull(chatId);
            Assert.IsNotNull(users);
            Assert.IsTrue(users.Length > 0);
            return hub.InvokeAsync("AddParticipants", chatId, new
            {
                Ids = users
            });
        }

        public static Task LeaveChat(this HubConnection hub, string chatId)
        {
            Assert.IsNotNull(hub);
            Assert.IsNotNull(chatId);
            return hub.InvokeAsync("LeaveChat", chatId);
        }

        public static async Task StatTyping(this HubConnection hub, string chatId)
        {
            await hub.InvokeAsync<JObject>("StartTyping", chatId);
        }

        public static async Task StopTyping(this HubConnection hub, string chatId)
        {
            await hub.InvokeAsync<JObject>("StopTyping", chatId);
        }

        public static async Task<DateTime> SendMessage(this HubConnection hub, string chatId, string message)
        {
            Assert.IsNotNull(chatId);
            Assert.IsNotNull(message);

            return await hub.InvokeAsync<DateTime>("SendMessage", chatId, new InstantMessage
            {
                Message = message
            });
        }

        public static async Task<IEnumerable<InstantMessage>> GetMessages(this HubConnection hub, string chatId, DateTime? from = null,
            DateTime? to = null, int count = -1, bool moveBackward = false)
        {
            Assert.IsNotNull(hub);
            Assert.IsNotNull(chatId);
            return await hub.InvokeAsync<IEnumerable<InstantMessage>>("GetMessages", chatId, from, to, count, moveBackward);
        }

        public static Task<JObject> Ping(this HubConnection hub)
        {
            return hub.InvokeAsync<JObject>("Ping");
        }
    }
}
using System;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Transport;

namespace Cactus.Chat
{
    public class NullClientEndpoint : IChatClientEndpoint
    {
        public Task MessageRead(string chatId, string userId, DateTime timestamp)
        {
            return Task.CompletedTask;
        }

        public Task MessageDelivered(string chatId, string userId, DateTime timestamp)
        {
            return Task.CompletedTask;
        }

        public Task MessageNew<T>(string chatId, T message) where T : InstantMessage
        {
            return Task.CompletedTask;
        }

        public Task ParticipantAdded<T>(string chatId, string userId, string participantId, T profile) where T : IChatProfile
        {
            return Task.CompletedTask;
        }

        public Task ParticipantLeft(string chatId, string userId)
        {
            return Task.CompletedTask;
        }

        public Task ParticipantStartTyping(string chatId, string userId)
        {
            return Task.CompletedTask;
        }

        public Task ParticipantStopTyping(string chatId, string userId)
        {
            return Task.CompletedTask;
        }

        public Task ChatTitleChanged(string chatId, string userId, string title)
        {
            return Task.CompletedTask;
        }

        public Task UserConnected(string userId)
        {
            return Task.CompletedTask;
        }

        public Task UserDisconnected(string userId)
        {
            return Task.CompletedTask;
        }
    }
}
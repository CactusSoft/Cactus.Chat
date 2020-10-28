using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Transport.Models.Output;
using Netcore.Simplest.Chat.Models;
using StreamJsonRpc;

namespace Netcore.Simplest.Chat.Test
{
    internal class ChatEvent
    {
        public ChatEvent(string method)
        {
            Method = method;
            Timestamp = DateTime.UtcNow;
        }
        public ChatEvent(string method, string chatId) : this(method)
        {
            ChatId = chatId;
        }

        public string Method { get; set; }
        public DateTime Timestamp { get; set; }
        public string ChatId { get; set; }

        public override string ToString()
        {
            return Method;
        }
    }

    internal class StackChatClientEndpoint
    {
        private ConcurrentStack<ChatEvent> _events = new ConcurrentStack<ChatEvent>();

        public ConcurrentStack<ChatEvent> EventStack => _events;

        [JsonRpcMethod("messageRead")]
        public Task MessageRead(MessageIdentifierNotification msg)

        {
            _events.Push(new ChatEvent("messageRead", msg.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("messageDelivered")]
        public Task MessageDelivered(MessageIdentifierNotification msg)
        {
            _events.Push(new ChatEvent("messageDelivered", msg.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("messageNew")]
        public Task MessageNew(MessageNewNotification<InstantMessage> dto)
        {
            _events.Push(new ChatEvent("messageNew", dto.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("participantAdded")]
        public Task ParticipantAdded<T>(ParticipantAddedNotification<CustomProfile> msg) where T : IChatProfile
        {
            _events.Push(new ChatEvent("participantAdded", msg.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("participantLeft")]
        public Task ParticipantLeft(ParticipantIdentifierNotification msg)
        {
            _events.Push(new ChatEvent("participantLeft", msg.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("participantStartTyping")]
        public Task ParticipantStartTyping(ParticipantIdentifierNotification msg)
        {
            _events.Push(new ChatEvent("participantStartTyping", msg.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("participantStopTyping")]
        public Task ParticipantStopTyping(ParticipantIdentifierNotification msg)
        {
            _events.Push(new ChatEvent("participantStopTyping", msg.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("chatTitleChanged")]
        public Task ChatTitleChanged(ChatTitleChangedNotification msg)
        {
            _events.Push(new ChatEvent("chatTitleChanged", msg.ChatId));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("userConnected")]
        public Task UserConnected(UserIdentifierNotification msg)
        {
            _events.Push(new ChatEvent("userConnected"));
            return Task.CompletedTask;
        }

        [JsonRpcMethod("userDisconnected")]
        public Task UserDisconnected(UserIdentifierNotification msg)
        {
            _events.Push(new ChatEvent("userDisconnected"));
            return Task.CompletedTask;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.Events;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Signalr.Connections;
using Cactus.Chat.Transport;
using Cactus.Chat.Transport.Models.Input;
using Cactus.Chat.Transport.Models.Output;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Cactus.Chat.Signalr
{
    /// <summary>
    /// It's abstract cause of it's generic. SignalR doesn't support generic hubs.
    /// You should inherit concreate non-generic implementation 
    /// </summary>
    /// <typeparam name="T1">Chat</typeparam>
    /// <typeparam name="T2">Instant message</typeparam>
    /// <typeparam name="T3">User profile</typeparam>
    /// <typeparam name="T4"></typeparam>
    public abstract class AbstractChatHub<T1, T2, T3, T4> : Hub, IChatServerEndpoint<T1, T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
        where T4 : Hub
    {
        private readonly ILogger _log;
        private readonly IChatService<T1, T2, T3> _chatService;
        private readonly IConnectionStorage _connectionStorage;
        private readonly IEventHub _bus;

        protected AbstractChatHub(IChatService<T1, T2, T3> chatService, IConnectionStorage connectionStorage,
            IEventHub bus, ILogger log)
        {
            _log = log;
            _chatService = chatService;
            _connectionStorage = connectionStorage;
            _bus = bus;
            _log.LogDebug(".ctor");
        }

        public async Task<Ping> Ping()
        {
            return new Ping
            {
                ChatService = _chatService.GetType().Assembly.FullName,
                Executable = Assembly.GetEntryAssembly()?.FullName,
                Storage = await _chatService.GetStorageInfo(),
                IsAuthenticated = Context.User.Identity.IsAuthenticated,
                UserId = Context.User.Identity.IsAuthenticated ? AuthContext.GetUserId() : null,
                Timestamp = DateTime.UtcNow,
            };
        }

        public async Task<DateTime> SendMessage(string chatId, T2 message)
        {
            var ctx = AuthContext;
            _log.LogInformation("SendMessage(chatId:{chat_Id}) [{user_id}]", chatId, ctx.GetUserId());
            await TranslateExceptionIfFailAsync(async () => { await _chatService.SendMessage(ctx, chatId, message); });

            _log.LogDebug("Message sent to chatId {chat_id}, returns {timestamp}", chatId, message.Timestamp);
            return message.Timestamp;
        }

        public Task<IEnumerable<UserStatus>> GetUserStatus(string[] userIds)
        {
            var ctx = AuthContext;
            _log.LogInformation("GetUserStatus() [{user_id}]", ctx.GetUserId());
            var me = _connectionStorage.Get(ctx.ConnectionId);
            if (userIds == null || userIds.Length == 0)
                return Task.FromResult(Enumerable.Empty<UserStatus>());
            return Task.FromResult(userIds.Select(id => new UserStatus
            {
                Id = id,
                IsOnline = _connectionStorage.ToEnumerable()
                    .Where(e => e.BroadcastGroup == me.BroadcastGroup)
                    .Any(e => e.UserId == id)
            }));
        }

        public async Task<ChatSummary<T2, T3>> StartChat(T1 chat)
        {
            var ctx = AuthContext;
            _log.LogInformation("StartChat() [{user_id}]", ctx.GetUserId());
            await TranslateExceptionIfFailAsync(async () => { chat = await _chatService.StartChat(AuthContext, chat); });

            _log.LogInformation("Chat started, id: {chat_id}", chat.Id);
            return BuildChatDto(chat, AuthContext.GetUserId());
        }

        public async Task<IEnumerable<ChatSummary<T2, T3>>> GetChats()
        {
            var ctx = AuthContext;
            _log.LogInformation("GetChats() [{user_id}]", ctx.GetUserId());
            var userId = ctx.GetUserId();
            var res = await _chatService.Get(ctx);
            return res.Select(e => BuildChatDto(e, userId));
        }

        public async Task<ChatSummary<T2, T3>> GetChat(string id)
        {
            var ctx = AuthContext;
            _log.LogInformation("GetChat(id:{chat_id}) [{user_id}]", id, ctx.GetUserId());
            var res = await _chatService.Get(ctx, id);
            var dto = BuildChatDto(res, ctx.GetUserId());
            return dto;
        }

        public async Task<IEnumerable<T2>> GetMessages(string chatId, DateTime? from = null,
            DateTime? to = null, int count = -1, bool moveBackward = false)
        {
            var ctx = AuthContext;
            _log.LogInformation(
                "GetMessages(chatId:{chat_id}, from:{from}, to:{to}, count:{count}, moveBackward:{move_backward}) [{user_id}]",
                chatId, from, to, count, moveBackward, ctx.GetUserId());
            IEnumerable<T2> res;
            if (moveBackward)
            {
                res = await _chatService.GetMessageHistory(
                    ctx,
                    chatId,
                    @from ?? DateTime.MaxValue,
                    to ?? DateTime.MinValue,
                    count <= 0 ? 30 : count,
                    moveBackward);
            }
            else
            {
                res = await _chatService.GetMessageHistory(
                    ctx,
                    chatId,
                    @from ?? DateTime.MinValue,
                    to ?? DateTime.MaxValue,
                    count <= 0 ? 30 : count,
                    moveBackward);
            }

            return res;
        }

        public async Task ChangeTitle(string chatId, string title)
        {
            var ctx = AuthContext;
            _log.LogInformation("ChangeTitle(chatId:{chat_id}, title:{chat_title}) [{user_id}]", chatId, title,
                ctx.GetUserId());
            await _chatService.ChangeTitle(ctx, chatId, title);
        }

        public async Task LeaveChat(string chatId)
        {
            var ctx = AuthContext;
            _log.LogInformation("LeaveChat(chatId:{chat_id}) [{user_id}]", chatId, ctx.GetUserId());
            await TranslateExceptionIfFailAsync(async () => { await _chatService.LeaveChat(ctx, chatId); });
        }

        public async Task Read(string chatId, DateTime timestamp)
        {
            var ctx = AuthContext;
            _log.LogInformation("Read(chatId:{chat_id}, timestamp:{timestamp}) [{user_id)}]", chatId, timestamp,
                ctx.GetUserId());
            await TranslateExceptionIfFailAsync(async () => { await _chatService.MarkRead(ctx, chatId, timestamp); });
        }

        public async Task ReadAll(DateTime timestamp)
        {
            var ctx = AuthContext;
            _log.LogInformation("ReadAll(timestamp:{timestamp}) [{user_id}]", timestamp, ctx.GetUserId());
            await TranslateExceptionIfFailAsync(async () => { await _chatService.MarkReadBulk(ctx, timestamp); });
        }

        public async Task Received(string chatId, DateTime timestamp)
        {
            var ctx = AuthContext;
            _log.LogInformation("Received(chatId:{chat_id}, timestamp:{timestamp}) [{user_id}]", chatId, timestamp,
                ctx.GetUserId());
            await TranslateExceptionIfFailAsync(async () => { await _chatService.MarkDelivered(ctx, chatId, timestamp); });
        }

        public async Task AddParticipants(string chatId, AddParticipantsCommand participants)
        {
            var ctx = AuthContext;
            _log.LogInformation("AddParticipants(chatId:{chat_id}) [{user_id}]", chatId, ctx.GetUserId());
            await TranslateExceptionIfFailAsync(async () =>
            {
                Validate.NotNull(participants);
                await _chatService.AddParticipants(ctx, chatId, participants.Ids);
            });
        }

        public async Task StartTyping(string chatId)
        {
            var ctx = AuthContext;
            _log.LogInformation("StartTyping(chatId:{chat_id}) [{user_id}]", chatId, ctx.GetUserId());
            var userId = AuthContext.GetUserId();
            await _bus.FireEvent(new ParticipantStartTyping
                {ChatId = chatId, UserId = userId, ConnectionId = ctx.ConnectionId});
        }

        public async Task StopTyping(string chatId)
        {
            var ctx = AuthContext;
            _log.LogInformation("AddParticipants(chatId:{chat_id}) [{user_id}]", chatId, ctx.GetUserId());
            var userId = AuthContext.GetUserId();
            await _bus.FireEvent(new ParticipantStopTyping
                {ChatId = chatId, UserId = userId, ConnectionId = ctx.ConnectionId});
        }

        public Task<IEnumerable<string>> GetContactsOnline()
        {
            var ctx = AuthContext;
            _log.LogInformation("GetContactsOnline() [{user_id}]", ctx.GetUserId());
            var res = _connectionStorage.ToEnumerable()
                .Where(e => e.BroadcastGroup == GetBroadcastGroup(ctx.Identity as IIdentity))
                .Select(e => e.UserId)
                .Distinct();
            return Task.FromResult(res);
        }

        public override async Task OnConnectedAsync()
        {
            var ctx = AuthContext;
            var userId = ctx.GetUserId();
            _log.LogInformation("OnConnected() [{user_id}]", ctx.GetUserId());
            var broadcastGroup = BroadcastGroup;
            var connection = new ConnectionInfo(ConnectionId, userId, broadcastGroup,
                new ClientEndpoint<T4>(HubContext, ConnectionId, _log));
            _connectionStorage.Add(connection);
            await _bus.FireEvent(new UserConnected
                {ConnectionId = ConnectionId, UserId = userId, BroadcastGroup = broadcastGroup});
            _log.LogDebug("User {user_id} has been connected successfully", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var ctx = AuthContext;
            var userId = ctx.GetUserId();
            _log.LogInformation("OnDisconnected [{connection_id} : {user_id}], exception:{exception})", ConnectionId,
                userId, exception);
            var info = _connectionStorage.Delete(ConnectionId);
            await _bus.FireEvent(new UserDisconnected
                {ConnectionId = info?.Id, UserId = userId, BroadcastGroup = info?.BroadcastGroup});
            _log.LogInformation("User disconnected successfully {connection_id} : {user_id} / {broadcast_group}",
                info?.Id, userId, info?.BroadcastGroup);
            await base.OnDisconnectedAsync(exception);
        }

        protected virtual string GetBroadcastGroup(IIdentity identity)
        {
            return null;
        }

        protected abstract IAuthContext AuthContext { get; }

        protected string BroadcastGroup => GetBroadcastGroup(Context.User.Identity);

        protected string ConnectionId => Context.ConnectionId;

        protected virtual ChatSummary<T2, T3> BuildChatDto(T1 e, string currentUserId)
        {
            return new ChatSummary<T2, T3>
            {
                LastMessage = e.Messages.LastOrDefault(),
                Participants = e.Participants.Select(p => new ParticipantSummary<T3>
                {
                    Id = p.Id,
                    Profile = p.Profile,
                    IsDeleted = p.IsDeleted,
                    HasLeft = p.HasLeft,
                    ReadOn = p.ReadOn,
                    DeliveredOn = p.DeliveredOn,
                    LastMessageOn = p.LastMessageOn,
                    IsOnline = _connectionStorage.ToEnumerable().Any(x => x.UserId == p.Id)
                }).ToList(),
                ReadOn = e.Participants.FirstOrDefault(p => p.Id == currentUserId)?.ReadOn,
                MessageCount = e.MessageCount,
                StartedBy = e.StartedBy,
                StartedOn = e.StartedOn,
                Id = e.Id,
                Title = e.Title
            };
        }

        protected virtual async Task TranslateExceptionIfFailAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _log.LogError(ex.ToString());
                throw BuildException(ex);
            }
        }

        protected virtual Exception BuildException(Exception input)
        {
            return new ErrorInfo(input.Message, 0xDEAD);
        }

        protected abstract IHubContext<T4> HubContext { get; }
    }

    // AspNetCore SignalR version doesn't support custom error data with ChatHub.
    // It's brilliant opportunity to build a custom bike with square wheels!
    public sealed class ErrorInfo : Exception
    {
        public ErrorInfo(string message, int code) : base($"{{\"Message\":\"{message}\",\"Code\":{code}}}")
        {
        }
    }
}
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
using Cactus.Chat.Logging;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Signalr.Connections;
using Cactus.Chat.Transport;
using Cactus.Chat.Transport.Models.Input;
using Cactus.Chat.Transport.Models.Output;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

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
        private readonly ILog _log = LogProvider.GetLogger("Cactus.Chat.Signalr.AbstractChatHub");
        private readonly IChatService<T1, T2, T3> _chatService;
        private readonly IConnectionStorage _connectionStorage;
        private readonly IEventHub _bus;

        protected AbstractChatHub(IChatService<T1, T2, T3> chatService, IConnectionStorage connectionStorage, IEventHub bus)
        {
            this._chatService = chatService;
            this._connectionStorage = connectionStorage;
            this._bus = bus;
            _log.Debug(".ctor");
        }

        public async Task<Ping> Ping()
        {
            return new Ping
            {
                ChatService = _chatService.GetType().Assembly.FullName,
                Executable = Assembly.GetEntryAssembly().FullName,
                Storage = await _chatService.GetStorageInfo(),
                IsAuthenticated = Context.User.Identity.IsAuthenticated,
                UserId = Context.User.Identity.IsAuthenticated ? AuthContext.GetUserId() : null,
                Timestamp = DateTime.UtcNow,
            };
        }

        public async Task<DateTime> SendMessage(string chatId, T2 message)
        {
            var ctx = AuthContext;
            _log.Info(() => $"SendMessage(chatId:{chatId}) [{ctx.GetUserId()}]");
            _log.Debug(() => $"Message: {JsonConvert.SerializeObject(message, Formatting.Indented)}");
            await TranslateExceptionIfFail(async () =>
            {
                await _chatService.SendMessage(ctx, chatId, message);
            });

            _log.DebugFormat("Message sent to chatId {0}, returns {1}", chatId, message.Timestamp);
            return message.Timestamp;
        }

        public async Task<ChatSummary<T2, T3>> StartChat(T1 chat)
        {
            var ctx = AuthContext;
            _log.Info(() => $"StartChat() [{ctx.GetUserId()}]");
            _log.Debug(() => $"Payload: {JsonConvert.SerializeObject(chat, Formatting.Indented)}");
            await TranslateExceptionIfFail(async () =>
            {
                chat = await _chatService.StartChat(AuthContext, chat);
            });

            _log.InfoFormat("Chat started, id: {0}", chat.Id);
            _log.Debug(() => $"Result chat: {JsonConvert.SerializeObject(chat, Formatting.Indented)}");

            return BuildChatDto(chat, AuthContext.GetUserId());
        }

        public async Task<IEnumerable<ChatSummary<T2, T3>>> GetChats()
        {
            var ctx = AuthContext;
            _log.Info(() => $"GetChats() [{ctx.GetUserId()}]");
            var userId = ctx.GetUserId();
            var res = await _chatService.Get(ctx);
            return res.Select(e => BuildChatDto(e, userId));
        }

        public async Task<ChatSummary<T2, T3>> GetChat(string id)
        {
            var ctx = AuthContext;
            _log.Info(() => $"GetChat(id:{id}) [{ctx.GetUserId()}]");
            var res = await _chatService.Get(ctx, id);
            var dto = BuildChatDto(res, ctx.GetUserId());
            _log.Debug(() => $"Returned: {JsonConvert.SerializeObject(dto, Formatting.Indented)}");
            return dto;
        }

        public async Task<IEnumerable<T2>> GetMessages(string chatId, DateTime? from = null,
            DateTime? to = null, int count = -1, bool moveBackward = false)
        {
            var ctx = AuthContext;
            _log.Info(() => $"GetMessages(chatId:{chatId}, from:{from}, to:{to}, count:{count}, moveBackward:{moveBackward}) [{ctx.GetUserId()}]");
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

            if (_log.IsDebugEnabled())
            {
                var list = res.ToList();
                res = list;
                _log.Debug($"Result: {JsonConvert.SerializeObject(list, Formatting.Indented)}");
            }
            return res;
        }

        public async Task ChangeTitle(string chatId, string title)
        {
            var ctx = AuthContext;
            _log.Info(() => $"ChangeTitle(chatId:{chatId}, title:{title}) [{ctx.GetUserId()}]");
            await _chatService.ChangeTitle(ctx, chatId, title);
        }

        public async Task LeaveChat(string chatId)
        {
            var ctx = AuthContext;
            _log.Info(() => $"LeaveChat(chatId:{chatId}) [{ctx.GetUserId()}]");
            await TranslateExceptionIfFail(async () =>
            {
                await _chatService.LeaveChat(ctx, chatId);
            });
        }

        public async Task Read(string chatId, DateTime timestamp)
        {
            var ctx = AuthContext;
            _log.Info(() => $"Read(chatId:{chatId}, timestamp:{timestamp}) [{ctx.GetUserId()}]");
            await TranslateExceptionIfFail(async () =>
            {
                await _chatService.MarkRead(ctx, chatId, timestamp);
            });
        }

        public async Task ReadAll(DateTime timestamp)
        {
            var ctx = AuthContext;
            _log.Info(() => $"ReadAll(timestamp:{timestamp}) [{ctx.GetUserId()}]");
            await TranslateExceptionIfFail(async () =>
            {
                await _chatService.MarkReadBulk(ctx, timestamp);
            });
        }

        public async Task Received(string chatId, DateTime timestamp)
        {
            var ctx = AuthContext;
            _log.Info(() => $"Received(chatId:{chatId}, timestamp:{timestamp}) [{ctx.GetUserId()}]");
            await TranslateExceptionIfFail(async () =>
            {
                await _chatService.MarkDelivered(ctx, chatId, timestamp);
            });
        }

        public async Task AddParticipants(string chatId, AddParticipantsCommand participants)
        {
            var ctx = AuthContext;
            _log.Info(() => $"AddParticipants(chatId:{chatId}) [{ctx.GetUserId()}]");
            _log.Debug(() => $"Participants: {JsonConvert.SerializeObject(participants, Formatting.Indented)}");
            await TranslateExceptionIfFail(async () =>
            {
                Validate.NotNull(participants);
                await _chatService.AddParticipants(ctx, chatId, participants.Ids);
            });
        }

        public async Task StartTyping(string chatId)
        {
            var ctx = AuthContext;
            _log.Info(() => $"StartTyping(chatId:{chatId}) [{ctx.GetUserId()}]");
            var userId = AuthContext.GetUserId();
            await _bus.FireEvent(new ParticipantStartTyping { ChatId = chatId, UserId = userId, ConnectionId = ctx.ConnectionId });
        }

        public async Task StopTyping(string chatId)
        {
            var ctx = AuthContext;
            _log.Info(() => $"AddParticipants(chatId:{chatId}) [{ctx.GetUserId()}]");
            var userId = AuthContext.GetUserId();
            await _bus.FireEvent(new ParticipantStopTyping { ChatId = chatId, UserId = userId, ConnectionId = ctx.ConnectionId });
        }

        public Task<IEnumerable<string>> GetContactsOnline()
        {
            var ctx = AuthContext;
            _log.Info(() => $"GetContactsOnline() [{ctx.GetUserId()}]");
            var res = _connectionStorage.ToEnumerable()
                .Where(e => e.BroadcastGroup == GetBroadcastGroup(ctx.Identity as IIdentity))
                .Select(e => e.UserId)
                .Distinct();

            _log.Debug(() => "Result: " + res.Aggregate((current, next) => current + ", " + next));
            return Task.FromResult(res);
        }

        public override async Task OnConnectedAsync()
        {
            var ctx = AuthContext;
            var userId = ctx.GetUserId();
            _log.Info(() => $"OnConnected() [{ConnectionId} : {userId}]");
            var broadcastGroup = BroadcastGroup;
            var connection = new ConnectionInfo(ConnectionId, userId, broadcastGroup,
                new ClientEndpoint<T4>(HubContext, ConnectionId));
            _connectionStorage.Add(connection);
            await _bus.FireEvent(new UserConnected { ConnectionId = ConnectionId, UserId = userId, BroadcastGroup = broadcastGroup });
            _log.DebugFormat("User {0} has been connected successfully", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var ctx = AuthContext;
            var userId = ctx.GetUserId();
            _log.Info(() => $"OnDisconnected [{ConnectionId} : {userId}], exception:{exception})");
            var info = _connectionStorage.Delete(ConnectionId);
            await _bus.FireEvent(new UserDisconnected { ConnectionId = info?.Id, UserId = userId, BroadcastGroup = info?.BroadcastGroup });
            _log.InfoFormat("User disconnected successfully {0} : {1} / {2}", info?.Id, userId, info?.BroadcastGroup);
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

        protected async Task TranslateExceptionIfFail(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString);
                throw BuildException(ex);
            }
        }

        protected async Task<T> TranslateExceptionIfFail<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _log.Error(ex.ToString);
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

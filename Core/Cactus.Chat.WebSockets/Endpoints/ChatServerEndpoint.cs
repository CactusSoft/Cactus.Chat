using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.External;
using Cactus.Chat.Logging;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Transport;
using Cactus.Chat.Transport.Models.Input;
using Cactus.Chat.Transport.Models.Output;
using Newtonsoft.Json;
using StreamJsonRpc;

namespace Cactus.Chat.WebSockets.Endpoints
{
    public class ChatServerEndpoint<T1, T2, T3> : IChatServerEndpoint<T1, T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly ILog Log = LogProvider.GetLogger("Cactus.Chat.WebSockets.ChatServerEndpoint");
        private readonly IChatService<T1, T2, T3> _chatService;
        private readonly IAuthContext _authContext;
        private readonly IConnectionStorage _connectionStorage;

        public ChatServerEndpoint(IChatService<T1, T2, T3> chatService, IAuthContext authContext, IConnectionStorage connectionStorage)
        {
            Log.Debug(".ctor");
            _chatService = chatService;
            _authContext = authContext;
            _connectionStorage = connectionStorage;
        }

        public async Task<Ping> Ping()
        {
            Log.DebugFormat("Ping {0} : {1}", _authContext.ConnectionId, _authContext.GetUserId());
            var isAuthenticated = (_authContext.Identity as IIdentity)?.IsAuthenticated ?? false;
            return new Ping
            {
                ChatService = _chatService.GetType().Assembly.FullName,
                Executable = Assembly.GetEntryAssembly().FullName,
                Storage = await _chatService.GetStorageInfo(),
                IsAuthenticated = isAuthenticated,
                UserId = isAuthenticated ? _authContext.GetUserId() : null,
                Timestamp = DateTime.UtcNow,
            };
        }

        public async Task<DateTime> SendMessage(string chatId, T2 message)
        {
            Log.Info(() => $"SendMessage(chatId:{chatId}) [{_authContext.GetUserId()}]");
            Log.Debug(() => $"Message: {JsonConvert.SerializeObject(message, Formatting.Indented)}");
            await TranslateExceptionIfFailAsync(async () =>
            {
                await _chatService.SendMessage(_authContext, chatId, message);
            });


            Log.DebugFormat("Message sent to chatId {0}, returns {1}", chatId, message.Timestamp);
            return message.Timestamp;
        }

        public Task<IEnumerable<string>> GetContactsOnline()
        {
            var me = _connectionStorage.Get(_authContext.ConnectionId);
            return Task.FromResult(
                _connectionStorage
                    .ToEnumerable()
                    .Where(e => e.UserId != me.UserId)
                    .Where(e => e.BroadcastGroup == me.BroadcastGroup)
                    .Select(e => e.UserId)
                    .Distinct()
                    .ToList() //Make a copy, a kind of snapshot
                    as IEnumerable<string>);
        }

        public async Task<ChatSummary<T2, T3>> StartChat(T1 chat)
        {
            Log.Info(() => $"StartChat() [{_authContext.GetUserId()}]");
            Log.Debug(() => $"Payload: {JsonConvert.SerializeObject(chat, Formatting.Indented)}");
            await TranslateExceptionIfFailAsync(async () =>
            {
                chat = await _chatService.StartChat(_authContext, chat);
            });

            Log.InfoFormat("Chat started, id: {0}", chat.Id);
            Log.Debug(() => $"Result chat: {JsonConvert.SerializeObject(chat, Formatting.Indented)}");

            return BuildChatDto(chat, _authContext.GetUserId());
        }

        public async Task<IEnumerable<ChatSummary<T2, T3>>> GetChats()
        {
            Log.Info(() => $"GetChats() [{_authContext.GetUserId()}]");
            var userId = _authContext.GetUserId();
            var res = await _chatService.Get(_authContext);
            return res.Select(e => BuildChatDto(e, userId));
        }

        public async Task<ChatSummary<T2, T3>> GetChat(string id)
        {
            Log.Info(() => $"GetChat(id:{id}) [{_authContext.GetUserId()}]");
            var res = await _chatService.Get(_authContext, id);
            var dto = BuildChatDto(res, _authContext.GetUserId());
            Log.Debug(() => $"Returned: {JsonConvert.SerializeObject(dto, Formatting.Indented)}");
            return dto;
        }

        public async Task<IEnumerable<T2>> GetMessages(string chatId, DateTime? from = null,
            DateTime? to = null, int count = -1, bool moveBackward = false)
        {
            Log.Info(() => $"GetMessages(chatId:{chatId}, from:{from}, to:{to}, count:{count}, moveBackward:{moveBackward}) [{_authContext.GetUserId()}]");
            IEnumerable<T2> res;
            if (moveBackward)
            {
                res = await _chatService.GetMessageHistory(
                    _authContext,
                    chatId,
                    @from ?? DateTime.MaxValue,
                    to ?? DateTime.MinValue,
                    count <= 0 ? 30 : count,
                    moveBackward);
            }
            else
            {
                res = await _chatService.GetMessageHistory(
                    _authContext,
                    chatId,
                    @from ?? DateTime.MinValue,
                    to ?? DateTime.MaxValue,
                    count <= 0 ? 30 : count,
                    moveBackward);
            }

            if (Log.IsDebugEnabled())
            {
                var list = res.ToList();
                res = list;
                Log.Debug($"Result: {JsonConvert.SerializeObject(list, Formatting.Indented)}");
            }
            return res;
        }

        public async Task ChangeTitle(string chatId, string title)
        {
            Log.Info(() => $"ChangeTitle(chatId:{chatId}, title:{title}) [{_authContext.GetUserId()}]");
            await _chatService.ChangeTitle(_authContext, chatId, title);
        }

        public async Task LeaveChat(string chatId)
        {
            Log.Info(() => $"LeaveChat(chatId:{chatId}) [{_authContext.GetUserId()}]");
            await TranslateExceptionIfFailAsync(async () =>
            {
                await _chatService.LeaveChat(_authContext, chatId);
            });
        }

        public async Task Read(string chatId, DateTime timestamp)
        {
            Log.Info(() => $"Read(chatId:{chatId}, timestamp:{timestamp}) [{_authContext.GetUserId()}]");
            await TranslateExceptionIfFailAsync(async () =>
            {
                await _chatService.MarkRead(_authContext, chatId, timestamp);
            });
        }

        public async Task ReadAll(DateTime timestamp)
        {
            Log.Info(() => $"ReadAll(timestamp:{timestamp}) [{_authContext.GetUserId()}]");
            await TranslateExceptionIfFailAsync(async () =>
            {
                await _chatService.MarkReadBulk(_authContext, timestamp);
            });
        }

        public async Task Received(string chatId, DateTime timestamp)
        {
            Log.Info(() => $"Received(chatId:{chatId}, timestamp:{timestamp}) [{_authContext.GetUserId()}]");
            await TranslateExceptionIfFailAsync(async () =>
            {
                await _chatService.MarkDelivered(_authContext, chatId, timestamp);
            });
        }

        public async Task AddParticipants(string chatId, AddParticipantsCommand participants)
        {
            Log.Info(() => $"AddParticipants(chatId:{chatId}) [{_authContext.GetUserId()}]");
            Log.Debug(() => $"Participants: {JsonConvert.SerializeObject(participants, Formatting.Indented)}");
            await TranslateExceptionIfFailAsync(async () =>
            {
                Validate.NotNull(participants);
                await _chatService.AddParticipants(_authContext, chatId, participants.Ids);
            });
        }

        public async Task StartTyping(string chatId)
        {
            Log.Info(() => $"StartTyping(chatId:{chatId}) [{_authContext.GetUserId()}]");
            await _chatService.ParticipantStartTyping(_authContext, chatId);
        }

        public async Task StopTyping(string chatId)
        {
            Log.Info(() => $"AddParticipants(chatId:{chatId}) [{_authContext.GetUserId()}]");
            await _chatService.ParticipantStopTyping(_authContext, chatId);
        }

        //public IList<string> GetContactsOnline()
        //{
        //    Log.Info(() => $"GetContactsOnline() [{_authContext.GetUserId()}]");
        //    var res = connectionStorage.ToEnumerable()
        //        .Where(e => e.BroadcastGroup == GetBroadcastGroup(ctx.Identity as IIdentity))
        //        .Select(e => e.UserId)
        //        .Distinct().ToList();

        //    Log.Debug(() => "Result: " + res.Aggregate((current, next) => current + ", " + next));
        //    return res;
        //}

        public void Error()
        {
            throw BuildException(new ArgumentException());
        }

        protected async Task TranslateExceptionIfFailAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString);
                throw BuildException(ex);
            }
        }

        protected virtual Exception BuildException(Exception input)
        {
            if (input is ArgumentException)
            {
                return new LocalRpcException(input.Message, input)
                {
                    ErrorData = new
                    { Code = 0xDEAD, Message = "Something wrong with your arguments bro. THIS DATA IS CUSTOMIZABLE, CONTACT YOUR SERVER-SIDE DEVELOPERS." }
                };
            }

            return input;
        }

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
    }
}
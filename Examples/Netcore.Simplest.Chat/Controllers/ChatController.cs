using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cactus.Chat;
using Cactus.Chat.Core;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using log4net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netcore.Simplest.Chat.Integration;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.Controllers
{
    [Authorize]
    public class ChatController : ControllerBase
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ChatController));
        private readonly IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> chatService;
        private readonly IAuthContext authContext;

        public ChatController(IChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> chatService,
            IAuthContext authContext)
        {
            Log.Debug(".ctor");
            this.chatService = chatService;
            this.authContext = authContext;
        }

        [HttpGet]
        [Route("ping")]
        [AllowAnonymous]
        public dynamic Ping()
        {
            return new
            {
                UserId = AuthContext?.GetUserId(),
                User?.Identity?.Name,
                User?.Identity?.IsAuthenticated,
                User?.Identity?.AuthenticationType,
                Timestamp = DateTime.UtcNow
            };
        }

        [HttpPost]
        public async Task<dynamic> Post(Chat<CustomIm, CustomProfile> dto)
        {
            var chat = new Chat<CustomIm, CustomProfile> {Title = dto.Title, Messages = dto.Messages};
            if (dto.Participants.Count > 0)
            {
                chat.Participants = new List<ChatParticipant<CustomProfile>>(dto.Participants.Count);
                dto.Participants.ForEach(e => chat.Participants.Add(new ChatParticipant<CustomProfile> {Id = e.Id}));
            }

            chat = await chatService.StartChat(AuthContext, chat);
            return BuildChatDto(chat, AuthContext.GetUserId());
        }

        public async Task<IEnumerable<dynamic>> Get()
        {
            var userId = authContext.GetUserId();
            return (await chatService.Get(AuthContext))
                .Select(e => BuildChatDto(e, userId));
        }

        public async Task<dynamic> Get(string id)
        {
            var userId = authContext.GetUserId();
            var e = await chatService.Get(AuthContext, id);
            return BuildChatDto(e, userId);
        }

        [HttpGet]
        [Route("chat/{id}/messages")]
        public async Task<IEnumerable<CustomIm>> GetMessages(string id, DateTime? from = null, DateTime? to = null,
            int count = -1, int moveBackward = 0)
        {
            if (moveBackward <= 0)
            {
                return await chatService.GetMessageHistory(
                    AuthContext,
                    id,
                    @from ?? DateTime.MinValue,
                    to ?? DateTime.MaxValue,
                    count <= 0 ? 30 : count,
                    moveBackward > 0);
            }

            return await chatService.GetMessageHistory(
                AuthContext,
                id,
                @from ?? DateTime.MaxValue,
                to ?? DateTime.MinValue,
                count <= 0 ? 30 : count,
                moveBackward > 0);
        }

        [HttpPost]
        [Route("chat/{id}/messages")]
        public async Task<dynamic> PostMessage(string id, CustomIm message)
        {
            await chatService.SendMessage(AuthContext, id, message);
            return new {message.Timestamp};
        }

        [HttpPost]
        [Route("chat/{id}/reads")]
        public async Task<dynamic> ReadMessage(string id)
        {
            var res = await chatService.MarkRead(AuthContext, id, DateTime.UtcNow);
            return new {Timestamp = res};
        }

        [HttpPost]
        [Route("chat/{id}/participants")]
        public async Task AppParticipants([FromRoute]string id, [FromBody]string[] participantIds)
        {
            await chatService.AddParticipants(AuthContext, id, participantIds);
        }

        [HttpDelete]
        [Route("chat/{id}/participants/{participantId}")]
        public async Task<ActionResult> LeaveChat(string id, string participantId)
        {
            if (participantId != null && participantId == "me")
            {
                await chatService.LeaveChat(AuthContext, id);
                return Ok();
            }

            return BadRequest("Only 'me' is supported");
        }

        private static dynamic BuildChatDto(Chat<CustomIm, CustomProfile> e, string currentContactId)
        {
            return new
            {
                LastMessage = e.Messages.LastOrDefault(),
                Participants = e.Participants.Select(p => new
                {
                    p.Id,
                    p.IsDeleted,
                    p.HasLeft,
                    p.Profile,
                    p.ReadOn,
                    p.DeliveredOn
                }),
                e.Participants.First(p => p.Id == currentContactId).ReadOn,
                e.MessageCount,
                e.StartedBy,
                e.StartedOn,
                e.Id,
                e.Title,
            };
        }

        protected IAuthContext AuthContext => new AuthContext(User?.Identity);
    }
}
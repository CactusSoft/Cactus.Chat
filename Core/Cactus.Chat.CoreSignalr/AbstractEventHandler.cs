using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cactus.Chat.Connection;
using Cactus.Chat.Core;
using Cactus.Chat.Events;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Microsoft.Extensions.Logging;


namespace Cactus.Chat.Signalr
{
    public abstract class AbstractEventHandler<T1, T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        private readonly IChatService<T1, T2, T3> _chatService;
        private readonly IEventHub _bus;
        private readonly IConnectionStorage _connectionStorage;
        private readonly ILogger _log;

        protected AbstractEventHandler(IChatService<T1, T2, T3> chatService, IEventHub bus,
            IConnectionStorage connectionStorage, ILogger log)
        {
            _chatService = chatService;
            _bus = bus;
            _connectionStorage = connectionStorage;
            _log = log;
        }

        public async Task Handle(MessageRead message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            await Task.WhenAll((await GetConnected(message)).Select(e => e.Client.MessageRead(message)));
        }

        public async Task Handle(MessageDelivered message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            await Task.WhenAll((await GetConnected(message)).Select(e => e.Client.MessageDelivered(message)));
        }

        public async Task Handle(MessageNew<T2> message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            var chatParticipants = await _chatService.GetParticipants(message.ChatId);
            var activeParticipants = chatParticipants.Where(p => !p.HasLeft && !p.IsDeleted);
            var connectedClients = _connectionStorage.ToEnumerable()
                .Where(e => activeParticipants.Any(p => p.Id == e.UserId))
                .Where(e => e.Id != message.ConnectionId)
                .ToList();

            _log.LogDebug("{user_count} participants, {connection_count} clients connected", chatParticipants.Count, connectedClients.Count);
            if (connectedClients.Count > 0)
            {
                await Task.WhenAll((await GetConnected(message)).Select(e =>
                    e.Client.MessageNew(message.ChatId, message.Payload)));
            }

            // Notify all disconnected clients
            foreach (var participant in activeParticipants
                .Where(e => e.Id != message.UserId)
                .Where(e => connectedClients.All(p => p.UserId != e.Id)))
            {
                await _bus.FireEvent(new NotDelivery<MessageNew<T2>>
                {
                    Message = message,
                    Addressee = participant.Id
                });
            }
        }

        public async Task Handle(ParticipantAdded<T3> message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            await Task.WhenAll(
                (await GetConnected(message))
                .Select(e => e.Client.ParticipantAdded(message.ChatId, message.UserId, message.Participant.Id,
                    message.Participant.Profile)));
        }

        public async Task Handle(ParticipantLeftChat message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            await Task.WhenAll((await GetConnected(message)).Select(e => e.Client.ParticipantLeft(message)));
        }

        public async Task Handle(ParticipantStartTyping message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            await Task.WhenAll((await GetConnected(message)).Select(e => e.Client.ParticipantStartTyping(message)));
        }

        public async Task Handle(ParticipantStopTyping message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            await Task.WhenAll((await GetConnected(message)).Select(e => e.Client.ParticipantStopTyping(message)));
        }

        public async Task Handle(ChatTitleUpdated message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            await Task.WhenAll((await GetConnected(message)).Select(e =>
                e.Client.ChatTitleChanged(message.ChatId, message.UserId, message.Title)));
        }

        public async Task Handle(UserConnected message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            if (message.BroadcastGroup != null)
            {
                await _connectionStorage.ToEnumerable()
                    .Where(e => e.UserId != message.UserId && e.BroadcastGroup == message.BroadcastGroup)
                    .Select(e => e.Client.UserConnected(message.UserId))
                    .WhenAll();
            }
        }

        public async Task Handle(UserDisconnected message)
        {
            _log.LogDebug("Handle {message}",message.GetType().Name);
            if (message.ConnectionId == null)
            {
                _log.LogWarning("Unable to disconnect the right way, connectionId not found in storage");
                return;
            }

            // Notify other participants about the disconnect
            if (_connectionStorage.ToEnumerable().All(e => e.UserId != message.UserId))
            {
                if (message.BroadcastGroup != null)
                {
                    _log.LogInformation("Notify '{broadcast_group}' group about the disconnect", message.BroadcastGroup);
                    await _connectionStorage.ToEnumerable()
                        .Where(e => e.UserId != message.UserId && e.BroadcastGroup == message.BroadcastGroup)
                        .Select(e => e.Client.UserDisconnected(message.UserId))
                        .WhenAll();
                }
                else
                    _log.LogDebug("None notified about the disconnect because the user has no broadcast group");
            }
            else
                _log.LogDebug("None notified about the disconnect because the user is still connected with other device");
        }

        protected async Task<IEnumerable<IConnectionInfo>> GetConnected(IParticipantIdentifier message)
        {
            var chatParticipants = await _chatService.GetParticipants(message.ChatId);
            var connected = _connectionStorage.ToEnumerable()
                .Where(e => chatParticipants.Any(p => p.Id == e.UserId))
                .Where(e => e.Id != message.ConnectionId);

            if (_log.IsEnabled(LogLevel.Debug))
            {
                var list = connected.ToList();
                var x = new StringBuilder();
                x.Append(list.Count);
                x.Append(" connected participant found");
                if (list.Count > 0)
                {
                    x.Append(": ");
                    list.ForEach(e =>
                    {
                        x.AppendLine();
                        x.Append(e.Id);
                        x.Append(" : ");
                        x.Append(e.UserId);
                        x.Append(" / ");
                        x.Append(e.BroadcastGroup);
                    });
                }

                return list;
            }

            return connected;
        }
    }
}
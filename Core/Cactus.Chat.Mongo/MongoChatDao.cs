using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Storage;
using Cactus.Chat.Storage.Error;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cactus.Chat.Mongo
{
    public class MongoChatDao<T1, T2, T3> : IChatDao<T1, T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        protected readonly IMongoCollection<T1> ChatCollection;
        private readonly ILogger _log;

        public MongoChatDao(IMongoCollection<T1> chatCollection, ILogger log)
        {
            ChatCollection = chatCollection;
            _log = log;
        }

        public virtual async Task<IEnumerable<T1>> GetUserChatList(string userId,
            Expression<Func<T1, bool>> filter = null)
        {
            var qBuilder = Builders<T1>.Filter;
            var query = qBuilder.ElemMatch(e => e.Participants, e => e.Id == userId && !e.HasLeft && !e.IsDeleted);
            if (filter != null)
            {
                query = query & qBuilder.Where(filter);
            }

            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("Get chat list by query: {mongo_query}",
                    query.Render(
                        BsonSerializer.SerializerRegistry.GetSerializer<T1>(),
                        BsonSerializer.SerializerRegistry));
            }

            return (await ChatCollection.FindAsync(query)).ToEnumerable();
        }

        public virtual async Task<T1> Get(string chatId)
        {
            var res = await ChatCollection.Find(e => e.Id == chatId).FirstOrDefaultAsync();
            _ = res ?? throw new NotFoundException($"Chat {chatId} not found");
            return res;
        }

        public virtual async Task<T1> FindChatWithParticipants(string userId1, string userId2)
        {
            var qBuilder = Builders<T1>.Filter;
            var query = qBuilder.Size(e => e.Participants, 2) &
                        qBuilder.ElemMatch(e => e.Participants, e => e.Id == userId1) &
                        qBuilder.ElemMatch(e => e.Participants, e => e.Id == userId2);

            return await ChatCollection.Find(query).FirstOrDefaultAsync();
        }

        public virtual async Task Create(T1 chat)
        {
            await ChatCollection.InsertOneAsync(chat);
        }

        public virtual async Task AddMessage(string chatId, T2 msg)
        {
            var query =
                Builders<T1>.Filter.Eq(e => e.Id, chatId) &
                Builders<T1>.Filter.ElemMatch(e => e.Participants,
                    e => e.Id == msg.Author && (e.LastMessageOn < msg.Timestamp || e.LastMessageOn == null));
            var update = Builders<T1>.Update
                .Push(e => e.Messages, msg)
                .Inc(e => e.MessageCount, 1)
                .Set(e => e.LastActivityOn, msg.Timestamp)
                .Set(e => e.Participants[-1].LastMessageOn, msg.Timestamp);
            var res = await ChatCollection.UpdateOneAsync(query, update);
            if (res.MatchedCount == 0)
            {
                _log.LogWarning("Nothing is updated during adding message, maybe because of concurrent update");
                var count = await ChatCollection.CountDocumentsAsync(
                    Builders<T1>.Filter.Eq(e => e.Id, chatId) &
                    Builders<T1>.Filter.ElemMatch(e => e.Participants, e => e.Id == msg.Author)
                );
                if (count > 0) throw new ConcurrencyException();
                throw new NotFoundException($"Chat {chatId} wit active participant {msg.Author} not found");
            }
        }

        public virtual Task SetParticipantRead(string chatId, string userId, DateTime timestamp)
        {
            var qBuilder = Builders<T1>.Filter;
            var query = qBuilder.Eq(e => e.Id, chatId) & CreateUnreadChatFilter(userId, timestamp, qBuilder);
            var update = GetChatReadOnUpdateDefinition(timestamp);

            return ChatCollection.UpdateOneAsync(query, update);
        }

        public virtual async Task<IEnumerable<string>> SetParticipantReadAll(string userId, DateTime timestamp)
        {
            var qBuilder = Builders<T1>.Filter;
            var readQuery = CreateUnreadChatFilter(userId, timestamp, qBuilder);
            var update = GetChatReadOnUpdateDefinition(timestamp);

            var unreadChatsIds = await ChatCollection.Find(readQuery).Project(c => c.Id).ToListAsync();
            var updateFilter = readQuery & qBuilder.In(e => e.Id, unreadChatsIds);

            await ChatCollection.UpdateManyAsync(updateFilter, update);
            return unreadChatsIds;
        }

        private static UpdateDefinition<T1> GetChatReadOnUpdateDefinition(DateTime timestamp)
        {
            var uBuilder = Builders<T1>.Update;
            var update =
                uBuilder.Set(
                    nameof(Chat<InstantMessage, IChatProfile>.Participants) + ".$." +
                    nameof(ChatParticipant<IChatProfile>.ReadOn), timestamp);

            return update;
        }

        private static FilterDefinition<T1> CreateUnreadChatFilter(string userId, DateTime timestamp,
            FilterDefinitionBuilder<T1> qBuilder)
        {
            return qBuilder.ElemMatch(e => e.Participants,
                e => e.Id == userId && !e.HasLeft && !e.IsDeleted && (e.ReadOn < timestamp || e.ReadOn == null));
        }

        public virtual Task SetParticipantDelivered(string chatId, string userId, DateTime timestamp)
        {
            var qBuilder = Builders<T1>.Filter;
            var query = qBuilder.Eq(e => e.Id, chatId) & qBuilder.ElemMatch(e => e.Participants,
                e => e.Id == userId && !e.HasLeft && !e.IsDeleted &&
                     (e.DeliveredOn < timestamp || e.DeliveredOn == null));
            var uBuilder = Builders<T1>.Update;
            var update =
                uBuilder.Set(
                    nameof(Chat<InstantMessage, IChatProfile>.Participants) + ".$." +
                    nameof(ChatParticipant<IChatProfile>.DeliveredOn), timestamp);
            return ChatCollection.UpdateOneAsync(query, update);
        }

        public virtual Task SetParticipantLeft(string chatId, string userId)
        {
            return SetParticipantLeft(chatId, userId, true);
        }

        public virtual async Task SetParticipantLeft(string chatId, string userId, bool hasLeft)
        {
            var qBuilder = Builders<T1>.Filter;
            var query = qBuilder.Eq(e => e.Id, chatId) &
                        qBuilder.ElemMatch(e => e.Participants, e => e.Id == userId && !e.IsDeleted);
            var uBuilder = Builders<T1>.Update;
            var update =
                uBuilder.Set(
                    nameof(Chat<InstantMessage, IChatProfile>.Participants) + ".$." +
                    nameof(ChatParticipant<IChatProfile>.HasLeft), hasLeft);
            var res = await ChatCollection.UpdateOneAsync(query, update);
            if (res.MatchedCount == 0) throw new NotFoundException($"Chat {chatId} not found");
        }

        public virtual Task SetParticipantDeleted(string userId)
        {
            return SetParticipantDeleted(userId, true);
        }

        public virtual async Task SetParticipantDeleted(string userId, bool isDeleted)
        {
            var qBuilder = Builders<T1>.Filter;
            var query = qBuilder.ElemMatch(e => e.Participants, e => e.Id == userId && !e.IsDeleted);
            var uBuilder = Builders<T1>.Update;
            var update =
                uBuilder.Set(
                    nameof(Chat<InstantMessage, IChatProfile>.Participants) + ".$." +
                    nameof(ChatParticipant<IChatProfile>.IsDeleted), isDeleted);
            await ChatCollection.UpdateManyAsync(query, update);
        }

        public virtual async Task SetParticipants(string chatId, IList<ChatParticipant<T3>> participants)
        {
            var res = await ChatCollection.UpdateOneAsync(e => e.Id == chatId,
                Builders<T1>.Update.Set(e => e.Participants, participants));
            if (res.MatchedCount == 0) throw new NotFoundException($"Chat {chatId} not found");
        }

        public virtual async Task<IList<ChatParticipant<T3>>> GetParticipants(string chatId)
        {
            var res = await ChatCollection.Find(e => e.Id == chatId).Project(e => e.Participants).FirstOrDefaultAsync();
            if (res == null) throw new NotFoundException($"Chat {chatId} not found");
            return res;
        }

        public virtual async Task SetTitle(string chatId, string title)
        {
            var uBuilder = Builders<T1>.Update;
            var update = uBuilder.Set(e => e.Title, title);
            var res = await ChatCollection.UpdateOneAsync(e => e.Id == chatId, update);
            if (res.MatchedCount == 0) throw new NotFoundException($"Chat {chatId} not found");
        }

        public virtual async Task UpdateProfile(string userId, T3 profile)
        {
            var qBuilder = Builders<T1>.Filter;
            var query = qBuilder.ElemMatch(e => e.Participants, e => e.Id == userId && !e.IsDeleted && !e.HasLeft);
            var uBuilder = Builders<T1>.Update;
            var update =
                uBuilder.Set(
                    nameof(Chat<InstantMessage, IChatProfile>.Participants) + ".$." +
                    nameof(ChatParticipant<IChatProfile>.Profile), profile);
            await ChatCollection.UpdateManyAsync(query, update);
        }

        public virtual Task<string> GetInfo()
        {
            return Task.FromResult(
                $"MongoDB, assembly version {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}");
        }
    }
}
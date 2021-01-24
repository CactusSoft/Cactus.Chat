using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cactus.Chat;
using Cactus.Chat.Model;
using Cactus.Chat.Mongo;
using Cactus.Chat.Storage.Error;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mongo2Go;
using MongoDB.Bson;
using MongoDB.Driver;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.Test.Unit
{
    [TestClass]
    public class MongoStorageTest
    {
        private MongoDbRunner _mongo;
        private IMongoCollection<Chat<CustomIm, CustomProfile>> _chatCollection;

        [TestInitialize]
        public void SetUp()
        {
            _mongo = MongoDbRunner.Start();
            var client = new MongoClient(_mongo.ConnectionString).GetDatabase("test");
            _chatCollection = client.GetCollection<Chat<CustomIm, CustomProfile>>("chat");
        }

        [TestCleanup]
        public void Teardown()
        {
            _mongo?.Dispose();
        }

        [TestMethod]
        public async Task ConcurrencyExceptionTest()
        {
            var now = DateTime.UtcNow;
            var userId = ObjectId.GenerateNewId().ToString();
            var chat = new Chat<CustomIm, CustomProfile>
            {
                Participants = new[]
                {
                    new ChatParticipant<CustomProfile>
                    {
                        Id = userId,
                        LastMessageOn = now
                    },
                    new ChatParticipant<CustomProfile>
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        LastMessageOn = now.AddDays(-1)
                    }
                },
                Messages = new[]
                {
                    new CustomIm
                    {
                        Author = userId,
                        Timestamp = now
                    }
                }
            };
            await _chatCollection.InsertOneAsync(chat);
            var dao = new MongoChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>(_chatCollection,
                NullLogger.Instance);

            await Assert.ThrowsExceptionAsync<ConcurrencyException>(() =>
                dao.AddMessage(chat.Id, new CustomIm {Author = userId, Timestamp = now}));
            await Assert.ThrowsExceptionAsync<ConcurrencyException>(() =>
                dao.AddMessage(chat.Id, new CustomIm {Author = userId, Timestamp = now.AddSeconds(-1)}));
        }

        [TestMethod]
        public async Task AddMessageOkTest()
        {
            var now = DateTime.UtcNow;
            var userId = ObjectId.GenerateNewId().ToString();
            var chat = new Chat<CustomIm, CustomProfile>
            {
                Participants = new[]
                {
                    new ChatParticipant<CustomProfile>
                    {
                        Id = userId,
                        LastMessageOn = now
                    },
                    new ChatParticipant<CustomProfile>
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        LastMessageOn = now.AddDays(-1)
                    }
                },
                Messages = new[]
                {
                    new CustomIm
                    {
                        Author = userId,
                        Timestamp = now
                    }
                }
            };
            await _chatCollection.InsertOneAsync(chat);
            var dao = new MongoChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>(_chatCollection,
                NullLogger.Instance);
            await dao.AddMessage(chat.Id, new CustomIm {Author = userId, Timestamp = now.AddSeconds(1)});
        }

        [TestMethod]
        public async Task AddMessageByNotParticipantTest()
        {
            var now = DateTime.UtcNow;
            var userId = ObjectId.GenerateNewId().ToString();
            var chat = new Chat<CustomIm, CustomProfile>
            {
                Participants = new[]
                {
                    new ChatParticipant<CustomProfile>
                    {
                        Id = userId,
                        LastMessageOn = now
                    },
                    new ChatParticipant<CustomProfile>
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        LastMessageOn = now.AddDays(-1)
                    }
                },
                Messages = new[]
                {
                    new CustomIm
                    {
                        Author = userId,
                        Timestamp = now
                    }
                }
            };
            await _chatCollection.InsertOneAsync(chat);
            var dao = new MongoChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>(_chatCollection,
                NullLogger.Instance);
            await Assert.ThrowsExceptionAsync<NotFoundException>(() =>
                dao.AddMessage(chat.Id,
                    new CustomIm {Author = ObjectId.GenerateNewId().ToString(), Timestamp = now.AddSeconds(1)}));
        }
        
        [TestMethod]
        public async Task EmptyChatAddMessageTest()
        {
            var now = DateTime.UtcNow.RoundToMilliseconds();
            var userId = ObjectId.GenerateNewId().ToString();
            var chat = new Chat<CustomIm, CustomProfile>
            {
                Participants = new[]
                {
                    new ChatParticipant<CustomProfile>
                    {
                        Id = userId,
                    },
                    new ChatParticipant<CustomProfile>
                    {
                        Id = ObjectId.GenerateNewId().ToString()
                    }
                },
                Messages = new List<CustomIm>()
            };
            await _chatCollection.InsertOneAsync(chat);

            //act
            var dao = new MongoChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>(_chatCollection,
                NullLogger.Instance);
            await dao.AddMessage(chat.Id, new CustomIm {Author = userId, Timestamp = now});

            //assert
            var resChat = (await _chatCollection.FindAsync(e => e.Id == chat.Id)).First();
            Assert.AreEqual(1, resChat.MessageCount);
            Assert.AreEqual(now, resChat.LastActivityOn);
            Assert.AreEqual(now, resChat.Participants.First(e => e.Id == userId).LastMessageOn);
        }
    }
}
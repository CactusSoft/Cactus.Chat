using System;
using System.Threading.Tasks;
using Cactus.Chat.Core;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Storage;
using Cactus.Chat.Storage.Error;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using Moq;
using Netcore.Simplest.Chat.Models;

namespace Netcore.Simplest.Chat.Test.Unit
{
    [TestClass]
    public class ChatServiceTest
    {
        private ChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile> _chatService;
        private Mock<ISecurityManager<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>> _securityManagerMock;
        private Mock<IUserProfileProvider<CustomProfile>> _userProfileProviderMock;
        private Mock<IChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>> _chatDaoMock;
        private Mock<IEventHub> _eventHubMock;


        [TestInitialize]
        public void SetUp()
        {
            _securityManagerMock = new Mock<ISecurityManager<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>();
            _userProfileProviderMock = new Mock<IUserProfileProvider<CustomProfile>>();
            _chatDaoMock = new Mock<IChatDao<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>();
            _eventHubMock = new Mock<IEventHub>();
            _chatService = new ChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>(
                _securityManagerMock.Object,
                _userProfileProviderMock.Object,
                _chatDaoMock.Object,
                _eventHubMock.Object,
                NullLogger<ChatService<Chat<CustomIm, CustomProfile>, CustomIm, CustomProfile>>.Instance);
        }

        [TestMethod]
        public async Task AddMessageRetryTest()
        {
            var chatId = ObjectId.GenerateNewId().ToString();
            var userId = ObjectId.GenerateNewId().ToString();
            var chat = new Chat<CustomIm, CustomProfile>
            {
                Participants = new[]
                {
                    new ChatParticipant<CustomProfile>
                    {
                        Id = userId
                    },
                    new ChatParticipant<CustomProfile>
                    {
                        Id = ObjectId.GenerateNewId().ToString()
                    }
                }
            };
            var authContextMock = new Mock<IAuthContext>();
            authContextMock.Setup(e => e.GetUserId()).Returns(userId);
            _chatDaoMock.Setup(e => e.AddMessage(chatId, It.IsAny<CustomIm>())).Throws(new ConcurrencyException());
            _chatDaoMock.Setup(e => e.Get(chatId)).Returns(Task.FromResult(chat));
            _securityManagerMock.Setup(e => e.TrySendMessage(authContextMock.Object, chatId, It.IsAny<CustomIm>(),
                    It.IsAny<Lazy<Chat<CustomIm, CustomProfile>>>()))
                .Returns(Task.CompletedTask);

            //act
            await Assert.ThrowsExceptionAsync<ConcurrencyException>(() =>
                _chatService.SendMessage(authContextMock.Object, chatId,
                    new CustomIm {Author = userId, Message = "text"}));

            //assert
            _chatDaoMock.Verify(e => e.AddMessage(chatId, It.IsAny<CustomIm>()), Times.Exactly(3));
            _securityManagerMock.VerifyAll();
            authContextMock.VerifyAll();
        }
    }
}
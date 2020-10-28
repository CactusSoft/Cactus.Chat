using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;
using Cactus.Chat.Transport.Models.Input;
using Cactus.Chat.Transport.Models.Output;

namespace Cactus.Chat.Transport
{
    public interface IChatServerEndpoint<T1, T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        /// <summary>
        /// Retrieve general information about the server and check the connection at the same time
        /// </summary>
        /// <returns></returns>
        Task<Ping> Ping();

        /// <summary>
        /// Send message to chat
        /// </summary>
        /// <param name="chatId">Chat id</param>
        /// <param name="message">Message to send</param>
        /// <returns>Timestamp of added message</returns>
        Task<DateTime> SendMessage(string chatId, T2 message);

        /// <summary>
        /// Retrieve list of online users inside of your broadcast group
        /// </summary>
        /// <returns>List of user id</returns>
        Task<IEnumerable<string>> GetContactsOnline();

        /// <summary>
        /// Start new chat
        /// </summary>
        /// <param name="chat">Chat object, can contains initial messages</param>
        /// <returns>Summary information about the new chat</returns>
        Task<ChatSummary<T2, T3>> StartChat(T1 chat);

        /// <summary>
        /// Get chat list
        /// </summary>
        /// <returns>List of summary information</returns>
        Task<IEnumerable<ChatSummary<T2, T3>>> GetChats();

        /// <summary>
        /// Get single chat info
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Chat summary</returns>
        Task<ChatSummary<T2, T3>> GetChat(string id);

        /// <summary>
        /// Get chat messages. Returns any frame of chat stream based on income parameters.
        /// By default returns all messages from start to end
        /// </summary>
        /// <param name="chatId">Chat id</param>
        /// <param name="from">Minimal timestamp limit</param>
        /// <param name="to">Maximum timestamp limit</param>
        /// <param name="count">Maximum count of messages to receive, regardless reaching of timestamp limits</param>
        /// <param name="moveBackward">Moving direction. You can move from -> to direction (default) or backward: to -> from.</param>
        /// <returns>Message list</returns>
        Task<IEnumerable<T2>> GetMessages(string chatId, DateTime? from = null,
            DateTime? to = null, int count = -1, bool moveBackward = false);

        /// <summary>
        /// Change chat's title
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        Task ChangeTitle(string chatId, string title);

        /// <summary>
        /// Leave chat
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        Task LeaveChat(string chatId);

        /// <summary>
        /// Mark chat as read
        /// </summary>
        /// <param name="chatId">Chat id</param>
        /// <param name="timestamp">Timestamp of the latest read message</param>
        /// <returns></returns>
        Task Read(string chatId, DateTime timestamp);

        /// <summary>
        /// Mark all chats as read to the specified timestamp
        /// </summary>
        /// <param name="timestamp">Timestamp of the latest message that user read</param>
        /// <returns></returns>
        Task ReadAll(DateTime timestamp);

        /// <summary>
        /// Mark message as received
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        Task Received(string chatId, DateTime timestamp);

        /// <summary>
        /// Add new participant to chat
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="participants"></param>
        /// <returns></returns>
        Task AddParticipants(string chatId, AddParticipantsCommand participants);

        /// <summary>
        /// Notify other participants about start typing
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        Task StartTyping(string chatId);

        /// <summary>
        /// Notify other participants about stop typing
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        Task StopTyping(string chatId);
    }
}

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Storage
{
    public interface IChatDao<T1, in T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        /// <summary>
        /// Get list of chat where user is active participant
        /// </summary>
        /// <param name="userId">User id</param>
        /// <param name="filter">Additional filter</param>
        /// <returns>Chat collection that could be empty</returns>
        Task<IEnumerable<T1>> GetUserChatList(string userId, Expression<Func<T1, bool>> filter = null);

        /// <summary>
        /// Get chat by id
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns>Returns a chat or fails with InvalidOperationException if nothing found</returns>
        Task<T1> Get(string chatId);

        /// <summary>
        /// Looking for a chat with defined active participants
        /// </summary>
        /// <returns>A chat object or null</returns>
        Task<T1> FindChatWithParticipants(string userId1, string userId2);

        Task Create(T1 chat);

        /// <summary>
        /// Adds new message to the end of the chat
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        Task AddMessage(string chatId, T2 msg);

        /// <summary>
        /// Set ReadOn for specified participant
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="userId"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        Task SetParticipantRead(string chatId, string userId, DateTime timestamp);

        /// <summary>
        /// Set ReadOn for specified participant
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        Task<IEnumerable<string>> SetParticipantReadAll(string userId, DateTime timestamp);

        /// <summary>
        /// Set DeliveredOn field for participant
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="userId"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        Task SetParticipantDelivered(string chatId, string userId, DateTime timestamp);

        /// <summary>
        /// Mark participant as left the chat
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task SetParticipantLeft(string chatId, string userId);

        /// <summary>
        /// Mark participant as left the chat
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="userId"></param>
        /// <param name="hasLeft">If the user has left or not</param>
        /// <returns></returns>
        Task SetParticipantLeft(string chatId, string userId, bool hasLeft);

        /// <summary>
        /// Mark participant as deleted everywhere
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task SetParticipantDeleted(string userId);

        /// <summary>
        /// Mark participant as deleted everywhere
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="isDeleted">The value, if participant deleted or not</param>
        /// <returns></returns>
        Task SetParticipantDeleted(string userId, bool isDeleted);

        /// <summary>
        /// Set list of chat participants
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="participants"></param>
        /// <returns></returns>
        Task SetParticipants(string chatId, IList<ChatParticipant<T3>> participants);

        /// <summary>
        /// Get participant list
        /// </summary>
        /// <param name="chatId"></param>
        /// <returns></returns>
        Task<IList<ChatParticipant<T3>>> GetParticipants(string chatId);

        /// <summary>
        /// Set a chat title
        /// </summary>
        /// <param name="chatId">Chat Id</param>
        /// <param name="title">New title to set</param>
        /// <returns></returns>
        Task SetTitle(string chatId, string title);

        /// <summary>
        /// Update user profile in all chats
        /// </summary>
        /// <param name="userId">participant id</param>
        /// <param name="profile">new profile</param>
        /// <returns></returns>
        Task UpdateProfile(string userId, T3 profile);

        /// <summary>
        /// Returns general information about storage, like it's type and version.
        /// </summary>
        /// <returns></returns>
        Task<string> GetInfo();
    }
}

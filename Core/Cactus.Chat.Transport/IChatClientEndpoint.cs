using System;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Transport
{
    /// <summary>
    /// Represent a chat client endpoint interface,
    /// the methods that server can call on client-side 
    /// </summary>
    public interface IChatClientEndpoint
    {
        /// <summary>
        /// Notify that chat has been read by other user
        /// </summary>
        /// <param name="chatId">The chat id</param>
        /// <param name="userId">The user id who has read the chat</param>
        /// <param name="timestamp">Timestamp of read point</param>
        /// <returns></returns>
        Task MessageRead(string chatId, string userId, DateTime timestamp);

        /// <summary>
        /// Notify that a chat message has been delivered to other user
        /// </summary>
        /// <param name="chatId">The chat id</param>
        /// <param name="userId">The user id who has received the message</param>
        /// <param name="timestamp">the message timestamp</param>
        /// <returns></returns>
        Task MessageDelivered(string chatId, string userId, DateTime timestamp);

        /// <summary>
        /// Notify about the new message
        /// </summary>
        /// <typeparam name="T">Instant message</typeparam>
        /// <param name="chatId">chat id</param>
        /// <param name="message">the message</param>
        /// <returns></returns>
        Task MessageNew<T>(string chatId, T message) where T : InstantMessage;

        /// <summary>
        /// Notify that new participant has been added into the chat
        /// </summary>
        /// <typeparam name="T">Chat user profile</typeparam>
        /// <param name="chatId">chat id</param>
        /// <param name="userId">The user's id who has added the new participant</param>
        /// <param name="participantId">The new participant id</param>
        /// <param name="profile"></param>
        /// <returns></returns>
        Task ParticipantAdded<T>(string chatId, string userId, string participantId, T profile) where T : IChatProfile;

        /// <summary>
        /// Notify that a participant has left the chat
        /// </summary>
        /// <param name="chatId">chat id</param>
        /// <param name="userId">user id who has left</param>
        /// <returns></returns>
        Task ParticipantLeft(string chatId, string userId);

        /// <summary>
        /// Notify that a participant has started typing in the chat
        /// </summary>
        /// <param name="chatId">chat id</param>
        /// <param name="userId">user id who has started typing</param>
        /// <returns></returns>
        Task ParticipantStartTyping(string chatId, string userId);

        /// <summary>
        /// Notify that a participant has stopped typing in the chat
        /// </summary>
        /// <param name="chatId">chat id</param>
        /// <param name="userId">user id who has stopped typing</param>
        /// <returns></returns>
        Task ParticipantStopTyping(string chatId, string userId);

        /// <summary>
        /// Notify that a participant has changed the chat title
        /// </summary>
        /// <param name="chatId">chat id</param>
        /// <param name="userId">user id who has stopped typing</param>
        /// <param name="title">New chat title</param>
        /// <returns></returns>
        Task ChatTitleChanged(string chatId, string userId, string title);

        /// <summary>
        /// Notify that a user has connected to the server.
        /// This is the broadcast notification that usually spreading out inside of the user's BroadcastGroup only
        /// </summary>
        /// <param name="userId">connected user id</param>
        /// <returns></returns>
        Task UserConnected(string userId);

        /// <summary>
        /// Notify that a user has disconnected to the server.
        /// This is the broadcast notification that usually spreading out inside of the user's BroadcastGroup only
        /// </summary>
        /// <param name="userId">disconnected user id</param>
        /// <returns></returns>
        Task UserDisconnected(string userId);


    }
}

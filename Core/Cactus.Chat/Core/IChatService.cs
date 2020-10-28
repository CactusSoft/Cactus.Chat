using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cactus.Chat.External;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.Core
{
    public interface IChatService<T1, T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        /// <summary>
        /// Starts new chat. If chat contains any messages, they will be sent to participants. Current user must be a Contact.
        /// </summary>
        /// <param name="me"></param>
        /// <param name="chat"></param>
        Task<T1> StartChat(IAuthContext me, T1 chat);

        /// <summary>
        /// Send message to a chat. Current user must be a Contact.
        /// </summary>
        /// <param name="me">Athentication context</param>
        /// <param name="chatId">Chat identifier</param>
        /// <param name="message">Message to send</param>
        Task SendMessage(IAuthContext me, string chatId, T2 message);

        /// <summary>
        /// Get chat message history. Current user must be a Contact.
        /// </summary>
        /// <param name="me">Athentication context</param>
        /// <param name="chatId">Chat identifier</param>
        /// <param name="from">From date</param>
        /// <param name="to">To date</param>
        /// <param name="count"></param>
        /// <param name="moveBackward"></param>
        /// <returns>Returns all messages if @from or to are not specified</returns>
        Task<IEnumerable<T2>> GetMessageHistory(IAuthContext me, string chatId, DateTime from, DateTime to, int count, bool moveBackward);

        /// <summary>
        /// Get list of current user chats. Current user must be a Contact.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<T1>> Get(IAuthContext me, Expression<Func<T1, bool>> expression = null);

        /// <summary>
        /// Get a chat
        /// </summary>
        /// <returns>Chat object in case if you are the chat participant or EntityNotFoundException otherwise</returns>
        Task<T1> Get(IAuthContext me, string id);

        /// <summary>
        /// Get a chat participants
        /// </summary>
        /// <param name="chatId">Chat id</param>
        /// <returns>Full list of participants including left and deleted</returns>
        Task<IList<ChatParticipant<T3>>> GetParticipants(string chatId);

        /// <summary>
        /// Mark chat as read by current user. Current contact should be in participant's list
        /// </summary>
        /// <param name="me"></param>
        /// <param name="chatId">Chat id</param>
        /// <param name="timestamp"></param>
        Task<DateTime> MarkRead(IAuthContext me, string chatId, DateTime timestamp);

        /// <summary>
        /// Mark all chats of current user as read. Current contact should be in participant's list
        /// </summary>
        /// <param name="me"></param>
        /// <param name="timestamp"></param>
        Task<DateTime> MarkReadBulk(IAuthContext me, DateTime timestamp);

        /// <summary>
        /// Marks that chat has messages up to specified date delivered. Current contact should be in participant's list.
        /// </summary>
        Task<DateTime> MarkDelivered(IAuthContext me, string chatId, DateTime timestamp);

        /// <summary>
        /// Current user leavs a chat
        /// </summary>
        /// <param name="me"></param>
        /// <param name="chatId"></param>
        Task LeaveChat(IAuthContext me, string chatId);

        /// <summary>
        /// Add a few participants. If added participant has left the chat before, he will be get back to the chat. 
        /// </summary>
        /// <param name="me"></param>
        /// <param name="chatId">Chat id</param>
        /// <param name="participants">Collection of participant id</param>
        Task AddParticipants(IAuthContext me, string chatId, IEnumerable<string> participants);

        /// <summary>
        /// Change title to a new one
        /// </summary>
        /// <param name="me">Current user context</param>
        /// <param name="chatId">Chat id</param>
        /// <param name="title">New title</param>
        /// <returns>Throws exception if fails</returns>
        Task ChangeTitle(IAuthContext me, string chatId, string title);

        /// <summary>
        /// Returns general information about underline storage
        /// </summary>
        /// <returns>Some general storage info</returns>
        Task<string> GetStorageInfo();

        /// <summary>
        /// Participant just started typing into a chat
        /// </summary>
        /// <param name="me">Authentication context</param>
        /// <param name="chatId">Chat id</param>
        /// <returns></returns>
        Task ParticipantStartTyping(IAuthContext me, string chatId);

        /// <summary>
        /// Participant just stopped typing into a chat
        /// </summary>
        /// <param name="me">Authentication context</param>
        /// <param name="chatId">Chat id</param>
        /// <returns></returns>
        Task ParticipantStopTyping(IAuthContext me, string chatId);
    }
}

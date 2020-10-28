using System;
using System.Threading.Tasks;
using Cactus.Chat.Model;
using Cactus.Chat.Model.Base;

namespace Cactus.Chat.External
{
    /// <summary>
    /// Define securyty policy of the chat. It calls before doing an action and delegate security check.
    /// </summary>
    public interface ISecurityManager<T1, in T2, T3>
        where T1 : Chat<T2, T3>
        where T2 : InstantMessage
        where T3 : IChatProfile
    {
        /// <summary>
        /// Check if user may start a new chat, throws an exception if not
        /// </summary>
        /// <param name="me"></param>
        /// <param name="chat"></param>
        Task TryStart(IAuthContext me, T1 chat);

        /// <summary>
        /// Check if user can read the chat. Reading means read chat title, participants and all messages
        /// </summary>
        /// <returns></returns>
        Task TryRead(IAuthContext me, string chatId, Lazy<T1> chat);

        /// <summary>
        /// Check if user can write message to a paticular chat
        /// </summary>
        /// <param name="chat"></param>
        /// <param name="chatId"></param>
        /// <param name="msg"></param>
        Task TrySendMessage(IAuthContext me, string chatId, T2 msg, Lazy<T1> chat);

        /// <summary>
        /// Check if current user can add a new participant or return back ones who leave it
        /// </summary>
        /// <param name="chat"></param>
        /// <param name="chatId"></param>
        /// <param name="participantId"></param>
        /// <returns></returns>
        Task TryAddParticipant(IAuthContext me, string chatId, string participantId, Lazy<T1> chat);
    }
}

using System;

namespace Cactus.Chat.Events
{
    /// <summary>
    /// Identify a certain message from certain user in certain chat
    /// </summary>
    public interface IMessageIdentifier: IParticipantIdentifier
    {
        /// <summary>
        /// The message UTC timestamp
        /// </summary>
        DateTime Timestamp { get; }
    }
}

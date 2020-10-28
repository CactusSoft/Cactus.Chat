using Cactus.Chat.Transport;

namespace Cactus.Chat.Connection
{
    /// <summary>
    /// Immutable connection info bag. Hold it immutable.
    /// </summary>
    public interface IConnectionInfo
    {
        /// <summary>
        /// Connection identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// User identifier
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Broadcast chat group. *** It's NOT a security role or like this. ***
        /// Used to broadcast events like "get online, get offline, etc"
        /// </summary>
        string BroadcastGroup { get; }

        /// <summary>
        /// Client endpoint
        /// </summary>
        IChatClientEndpoint Client { get; }
    }
}

namespace Cactus.Chat.External
{
    /// <summary>
    /// Provides a way to get information about current user. Usually it works above standard Identity mechanism
    /// </summary>
    public interface IAuthContext
    {
        /// <summary>
        /// Original user identity object
        /// </summary>
        object Identity { get; }

        /// <summary>
        /// Connection identifier
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// Returns current participant ID (usually user ID received from authentication context)
        /// </summary>
        /// <returns>Unique identifier for a chat participant</returns>
        string GetUserId();
    }
}

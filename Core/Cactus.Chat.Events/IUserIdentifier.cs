namespace Cactus.Chat.Events
{
    public interface IUserIdentifier
    {
        /// <summary>
        /// User id
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// Connection id if applicable. Identify a certain connection that the message came from
        /// Could be null for transport that does not support a connection, for example HTTP.
        /// </summary>
        string ConnectionId { get; }
    }
}
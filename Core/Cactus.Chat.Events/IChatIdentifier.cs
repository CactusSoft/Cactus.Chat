namespace Cactus.Chat.Events
{
    /// <summary>
    /// Base interface for all events that occures in certain chat
    /// </summary>
    public interface IChatIdentifier
    {
        /// <summary>
        /// Chat id
        /// </summary>
        string ChatId { get; }
    }
}

namespace Cactus.Chat.Events
{
    /// <summary>
    /// Identify a certain chat participant
    /// </summary>
    public interface IParticipantIdentifier : IChatIdentifier, IUserIdentifier
    {
    }
}

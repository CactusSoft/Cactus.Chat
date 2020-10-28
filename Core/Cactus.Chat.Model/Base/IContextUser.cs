namespace Cactus.Chat.Model.Base
{
    public interface IContextUser<T> where T : IChatProfile
    {
        string Id { get; }

        bool IsDeleted { get; }

        T Profile { get; set; }
    }
}

namespace Cactus.Chat.Storage.Error
{
    public class NotFoundException : DataAccessException
    {
        public NotFoundException() : base("Entity not found")
        {
        }
    }
}
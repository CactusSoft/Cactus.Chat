namespace Cactus.Chat.Storage.Error
{
    public class NotFoundException : DataAccessException
    {
        public NotFoundException(string message) : base(message)
        {
        }

        public NotFoundException() : this("Entity not found")
        {
        }
    }
}
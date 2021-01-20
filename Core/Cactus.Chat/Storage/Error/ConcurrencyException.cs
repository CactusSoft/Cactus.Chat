using System;

namespace Cactus.Chat.Storage.Error
{
    public class ConcurrencyException : DataAccessException
    {
        public ConcurrencyException() : base("Concurrency issue")
        {
        }
    }
}
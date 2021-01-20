using System;

namespace Cactus.Chat.Storage.Error
{
    public class DataAccessException : Exception
    {
        protected DataAccessException()
        {
        }

        protected DataAccessException(string message) : base(message)
        {
        }
    }
}
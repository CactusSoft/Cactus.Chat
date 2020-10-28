using System.Collections.Generic;

namespace Cactus.Chat.Connection
{
    public interface IConnectionStorage
    {
        void Add(IConnectionInfo info);
        IConnectionInfo Delete(string connectionId);
        IConnectionInfo Get(string connectionId);
        IEnumerable<IConnectionInfo> ToEnumerable();
    }
}

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Cactus.Chat.Logging;

namespace Cactus.Chat.Connection
{
    public class ConcurrentDictionaryConnectionStorage : IConnectionStorage
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ConcurrentDictionaryConnectionStorage));
        private readonly ConcurrentDictionary<string, IConnectionInfo> _storage = new ConcurrentDictionary<string, IConnectionInfo>();

        public ConcurrentDictionaryConnectionStorage()
        {
            Log.Debug(".ctor");
        }

        public void Add(IConnectionInfo info)
        {
            var res = _storage.AddOrUpdate(info.Id, info, (key, val) =>
              {
                  Log.WarnFormat("Existing connection found {0} : {1} / {2}, replace it with a new one {3} / {4}",
                      key, val.UserId, val.BroadcastGroup, info.UserId, info.BroadcastGroup);
                  return info;
              });
            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat("New connection added: {0} : {1} / {2}", res.Id, res.UserId, res.BroadcastGroup);
                LogStorage();
            }
        }

        public IConnectionInfo Delete(string connectionId)
        {
            IConnectionInfo removedRec;
            _storage.TryRemove(connectionId, out removedRec);
            if (Log.IsDebugEnabled())
            {
                if (removedRec == null)
                    Log.DebugFormat("No connection found by id {0}", connectionId);
                else
                    Log.DebugFormat("Connection dropped {0} : {1} / {2} ", removedRec.Id, removedRec.UserId, removedRec.BroadcastGroup);
                LogStorage();
            }
            return removedRec;
        }

        public IConnectionInfo Get(string connectionId)
        {
            if (!_storage.TryGetValue(connectionId, out var val))
                Log.DebugFormat("Connection not found by key {0}", connectionId);
            return val;
        }

        public IEnumerable<IConnectionInfo> ToEnumerable()
        {
            return _storage.Values;
        }
        private void LogStorage()
        {
            var b = new StringBuilder();
            b.Append("Current connections:");
            if (_storage.Count == 0)
                b.Append(" <EMPTY>");
            else
                foreach (var item in _storage)
                {
                    b.AppendLine();
                    b.Append(item.Value.Id);
                    b.Append(" : ");
                    b.Append(item.Value.UserId);
                    b.Append(" / ");
                    b.Append(item.Value.BroadcastGroup);
                }
            Log.Debug(b.ToString);
        }
    }
}

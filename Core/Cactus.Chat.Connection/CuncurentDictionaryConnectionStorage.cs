using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Cactus.Chat.Connection
{
    public class ConcurrentDictionaryConnectionStorage : IConnectionStorage
    {
        private readonly ILogger<ConcurrentDictionaryConnectionStorage> _log;

        private readonly ConcurrentDictionary<string, IConnectionInfo> _storage =
            new ConcurrentDictionary<string, IConnectionInfo>();

        public ConcurrentDictionaryConnectionStorage(ILogger<ConcurrentDictionaryConnectionStorage> log)
        {
            _log = log;
            _log.LogDebug(".ctor");
        }

        public void Add(IConnectionInfo info)
        {
            var res = _storage.AddOrUpdate(info.Id, info, (key, val) =>
            {
                _log.LogWarning(
                    "Existing connection found {connection_id} : {user_id} / {broadcast_group}, replace it with a new one {user_id_new} / {broadcast_group_new}",
                    key, val.UserId, val.BroadcastGroup, info.UserId, info.BroadcastGroup);
                return info;
            });
            if (_log.IsEnabled(LogLevel.Debug))
            {
                _log.LogDebug("New connection added: {connection_id} : {user_id} / {broadcast_group}", res.Id,
                    res.UserId, res.BroadcastGroup);
                LogStorage();
            }
        }

        public IConnectionInfo Delete(string connectionId)
        {
            IConnectionInfo removedRec;
            _storage.TryRemove(connectionId, out removedRec);
            if (_log.IsEnabled(LogLevel.Debug))
            {
                if (removedRec == null)
                    _log.LogDebug("No connection found by id {connection_id}", connectionId);
                else
                    _log.LogDebug("Connection dropped {connection_id} : {user_id} / {broadcast_group}", removedRec.Id,
                        removedRec.UserId, removedRec.BroadcastGroup);
                LogStorage();
            }

            return removedRec;
        }

        public IConnectionInfo Get(string connectionId)
        {
            if (!_storage.TryGetValue(connectionId, out var val))
                _log.LogDebug("Connection not found by key {connection_id}", connectionId);
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

            _log.LogDebug(b.ToString());
        }
    }
}
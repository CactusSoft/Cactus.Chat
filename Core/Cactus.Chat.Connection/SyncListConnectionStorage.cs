using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cactus.Chat.Logging;

namespace Cactus.Chat.Connection
{
    public class SyncListConnectionStorage : IConnectionStorage
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(SyncListConnectionStorage));
        protected List<IConnectionInfo> Storage = new List<IConnectionInfo>();
        protected ReaderWriterLockSlim Locker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public SyncListConnectionStorage()
        {
            Log.Debug(".ctor");
        }

        public void Add(IConnectionInfo info)
        {
            if (info == null) throw new ArgumentException("info");
            Locker.EnterWriteLock();
            try
            {
                if (Log.IsDebugEnabled())
                {
                    Log.DebugFormat("Add new {0}:{1}", info.Id, info.UserId);
                    LogStorage();
                }
                Storage.Add(info);
            }
            finally { Locker.ExitWriteLock(); }
        }

        private void LogStorage()
        {
            var b = new StringBuilder();
            b.Append("Current connections:");
            if (Storage.Count == 0)
                b.Append(" <EMPTY>");
            else
                foreach (var item in Storage)
                {
                    b.AppendLine();
                    b.Append(item.Id);
                    b.Append(" : ");
                    b.Append(item.UserId);
                    b.Append(" / ");
                    b.Append(item.BroadcastGroup);
                }
            Log.Debug(b.ToString);
        }

        public IConnectionInfo Delete(string connectionId)
        {
            Locker.EnterWriteLock();
            try
            {
                if (Log.IsDebugEnabled())
                {
                    Log.DebugFormat("Try to delete {0}", connectionId);
                    LogStorage();
                }

                for (var i = 0; i < Storage.Count; i++)
                    if (Storage[i].Id == connectionId)
                    {
                        var droppedObj = Storage[i];
                        Storage.RemoveAt(i);
                        Log.DebugFormat("Found and dropped: {0}:{1}", droppedObj.Id, droppedObj.UserId);
                        return droppedObj;
                    }
                Log.Debug("Nothing found to delete");
                return null;
            }
            finally { Locker.ExitWriteLock(); }
        }

        public IConnectionInfo Get(string connectionId)
        {
            Locker.EnterReadLock();
            try
            {
                Log.DebugFormat("Get by id {0}", connectionId);
                var res = Storage.FirstOrDefault(e => e.Id == connectionId);
                if (res == null)
                {
                    Log.Debug("Nothing found");
                    LogStorage();
                }
                else
                {
                    Log.DebugFormat("Found {0}:{1}", res.Id, res.UserId);
                }
                return res;
            }
            finally { Locker.ExitReadLock(); }
        }

        public IEnumerable<IConnectionInfo> ToEnumerable()
        {
            Locker.EnterReadLock();
            try
            {
                Log.Debug("Enumerate storage");
                foreach (var item in Storage)
                {
                    yield return item;
                }
            }
            finally { Locker.ExitReadLock(); }
        }
    }
}
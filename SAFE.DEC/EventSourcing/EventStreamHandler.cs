using SAFE.EventSourcing;
using SAFE.EventSourcing.Models;
using SAFE.SystemUtils;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.EventSourcing.Stream
{
    // The main function of this class is to create a database if it does not exist.
    // So we might want to revise this implementation.
    public class EventStreamHandler : IEventStreamHandler
    {
        ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();

        readonly IEventStore _store;
        readonly string _databaseId;
        object _accessLock = new object();

        public EventStreamHandler(IEventStore store, string databaseId)
        {
            if (store == null || databaseId == null)
                throw new ArgumentNullException();

            _store = store;
            _databaseId = databaseId;
        }

        Task<Result<ReadOnlyStream>> IEventStreamHandler.GetStreamAsync(string streamKey, int newSinceVersion)
        {
            CheckCache(streamKey);
            return _store.GetStreamAsync(_databaseId, streamKey, newSinceVersion);
        }

        Task<Result<bool>> IEventStreamHandler.StoreBatchAsync(EventBatch batch)
        {
            CheckCache(batch.StreamKey);
            return _store.StoreBatchAsync(_databaseId, batch);
        }

        void CheckCache(string streamKey)
        {
            lock (_accessLock)
            {
                if (!_cache.TryGetValue(streamKey, out object data))
                {
                    // create db if not exists
                    var dbs = _store.GetDatabaseIdsAsync().GetAwaiter().GetResult();
                    if (!dbs.Any(d => d.Name == _databaseId))
                        _store.CreateDbAsync(_databaseId).GetAwaiter().GetResult();

                    _cache[streamKey] = new object();
                }
            }
        }
    }
}
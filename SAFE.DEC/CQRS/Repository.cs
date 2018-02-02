using SAFE.EventSourcing.Models;
using SAFE.EventSourcing;
using SAFE.SystemUtils;
using SAFE.SystemUtils.Events;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace SAFE.CQRS
{
    public class StreamVersion
    {
        public const int Any = -999;
        public const int NoStream = -1; // -1 means stream does not exist
        public const int Deleted = -2;
    }

    public class Repository
    {
        readonly IEventStreamHandler _streamHandler;
        readonly ConcurrentDictionary<string, Aggregate> _currentStateCache = new ConcurrentDictionary<string, Aggregate>();

        public Repository(IEventStreamHandler streamHandler)
        {
            _streamHandler = streamHandler;
        }

        /// <summary>
        /// Caches instances.
        /// Checks network for new events, 
        /// and applies them to cached instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="streamKey">[stream name]@[guid id]</param>
        /// <returns></returns>
        public async Task<T> GetAR<T>(string streamKey, int expectedVersion) where T : Aggregate
        {
            int cachedVersion = -1;
            if (_currentStateCache.TryGetValue(streamKey, out Aggregate cached))
                cachedVersion = cached.Version;

            var streamResult = await _streamHandler.GetStreamAsync(streamKey, cachedVersion); // todo: pass in cached state version, and only load newer versions

            var anyVersion = expectedVersion == StreamVersion.Any;
            var expectedAny = expectedVersion > StreamVersion.NoStream; // -1 means stream does not exist

            if (expectedAny && streamResult.Error)
                throw new Exception(streamResult.ErrorMsg);
            else if (!anyVersion && !expectedAny && streamResult.OK)
                throw new Exception("Stream already exists!");

            var stream = streamResult.Value;

            if (cached == null)
            {
                var ar = Activator.CreateInstance<T>();

                if (expectedAny)
                {
                    var events = stream.Data
                        .Select(x => x.GetDeserialized((b, t) => (Event)b.Parse(t)));

                    foreach (var e in events)
                        ar.BuildFromHistory(e);
                }

                _currentStateCache[streamKey] = ar;

                return ar;
            }
            else
            {
                var newEvents = stream.Data
                    .Where(d => d.MetaData.SequenceNumber > cached.Version)
                    .Select(x => x.GetDeserialized((b, t) => (Event)b.Parse(t)));

                foreach (var e in newEvents)
                    cached.BuildFromHistory(e);
            }

            // reconsider the location for these lines
            if (!anyVersion && cached.Version != expectedVersion) // protects AR from changes based on stale state.
                throw new InvalidOperationException($"Expected version {expectedVersion}, but stream has version {cached.Version}.");

            return (T)cached;
        }

        internal async Task<bool> Save(EventBatch batch)
        {
            var result = await _streamHandler.StoreBatchAsync(batch);

            return result.OK; // OK will be true also on idempotent writes
        }
    }   
}
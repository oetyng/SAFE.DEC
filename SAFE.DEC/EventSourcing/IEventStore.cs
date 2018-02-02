using System.Collections.Generic;
using System.Threading.Tasks;
using SAFE.EventSourcing.Models;
using SAFE.SystemUtils;

namespace SAFE.EventSourcing
{
    public interface IEventStore
    {
        Task CreateDbAsync(string databaseId);
        void Dispose();
        Task<List<DatabaseId>> GetDatabaseIdsAsync();

        /// <summary>
        /// Get a stream of events from the store.
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="streamKey"></param>
        /// <param name="newSinceVersion">If default value -1 is used (stream yet not existing), all events from stream will be loaded.</param>
        /// <returns></returns>
        Task<Result<ReadOnlyStream>> GetStreamAsync(string databaseId, string streamKey, int newSinceVersion = -1);
        Task<List<string>> GetStreamKeysAsync(string databaseId, string streamType);
        Task<List<string>> GetCategoriesAsync(string databaseId);
        Task<Result<bool>> StoreBatchAsync(string databaseId, EventBatch batch);
    }
}
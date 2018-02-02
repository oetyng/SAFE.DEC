using SAFE.EventSourcing.Models;
using SAFE.SystemUtils;
using System.Threading.Tasks;

namespace SAFE.EventSourcing
{
    public interface IEventStreamHandler
    {
        Task<Result<ReadOnlyStream>> GetStreamAsync(string streamKey, int newSinceVersion);
        Task<Result<bool>> StoreBatchAsync(EventBatch batch);
    }
}
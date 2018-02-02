using System;
using System.Collections.Generic;
using System.Linq;

namespace SAFE.EventSourcing.Models
{
    public abstract class ReadOnlyStream
    {
        public string StreamName { get; private set; }
        public long StreamId { get; private set; }
        public List<EventData> Data { get; private set; }

        public ReadOnlyStream(string streamName, long streamId, List<EventBatch> batches)
        {
            StreamName = streamName;
            StreamId = streamId;

            Data = batches
                .SelectMany(x => x.Body)
                .OrderBy(x => x.MetaData.SequenceNumber)
                .ToList();
        }
    }

    public class EmptyStream : ReadOnlyStream
    {
        public EmptyStream(string streamName, long streamId)
            : base(streamName, streamId, new List<EventBatch>())
        { }
    }

    public class PopulatedStream : ReadOnlyStream
    {
        public PopulatedStream(string streamName, long streamId, List<EventBatch> batches)
            : base(streamName, streamId, batches)
        {
            if (Data.Count != Data.Select(x => x.MetaData.SequenceNumber).Distinct().Count())
                throw new ArgumentException("Duplicate sequence numbers!");

            if (Data.First().MetaData.SequenceNumber != 0)
                throw new ArgumentException("Incomplete stream!");

            // Use the isInSequnce test done 
            // in EventBatch ctor to try 
            // if they all are in sequence.
            new EventBatch($"{streamName}@{streamId}", Guid.Empty, Data);
        }
    }
}

using System;

namespace SAFE.SystemUtils.Events
{
    /// <summary>
    /// OBSERVE: Properties cannot be of type IEnumerable
    /// they will not be deserialized if they are!
    /// </summary>
    public sealed class RaisedEvent
    {
        public RaisedEvent(Event @event)
        {
            Id = SequentialGuid.NewGuid();
            TimeStamp = SystemTime.UtcNow;
            Payload = @event.AsBytes();
            Name = @event.GetType().Name;
            EventClrType = @event.GetType().AssemblyQualifiedName;
        }

        public Guid Id { get; private set; }

        public DateTime TimeStamp { get; private set; }

        public byte[] Payload { get; private set; }

        public string Name { get; private set; }

        public string EventClrType { get; private set; }

        public int SequenceNumber { get; set; }
    }

    public abstract class Event
    {
    }
}
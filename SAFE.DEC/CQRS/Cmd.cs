using SAFE.SystemUtils;
using System;

namespace SAFE.CQRS
{
    public abstract class Cmd
    {
        public Guid Id { get; private set; }
        public long TargetId { get; private set; }
        public int ExpectedVersion { get; private set; }
        public Guid CorrelationId { get; protected set; }

        public Cmd(long targetId, int expectedVersion)
        {
            TargetId = targetId;
            Id = SequentialGuid.NewGuid();
            ExpectedVersion = expectedVersion;
        }
    }
}
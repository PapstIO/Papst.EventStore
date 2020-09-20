using System;

namespace Papst.EventStore.Abstractions
{
    internal class DependencyInjectionEventAggregatorStreamContext : IAggregatorStreamContext
    {
        public Guid StreamId { get; }

        public ulong TargetVersion { get; }

        public DependencyInjectionEventAggregatorStreamContext(Guid streamId, ulong targetVersion)
        {
            StreamId = streamId;
            TargetVersion = targetVersion;
        }
    }
}
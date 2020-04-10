using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Papst.EventStore.Abstractions
{
    class TypeBasedEventStreamApplier<TTargetType> : IEventStreamApplier<TTargetType>
        where TTargetType : class, new()
    {
        private readonly ILogger<TypeBasedEventStreamApplier<TTargetType>> _logger;

        public TypeBasedEventStreamApplier(ILogger<TypeBasedEventStreamApplier<TTargetType>> logger)
        {
            _logger = logger;
        }

        public TTargetType Apply(IEventStream stream) => Apply(stream, null);

        public TTargetType Apply(IEventStream stream, TTargetType target)
        {
            using (_logger.BeginScope($"Stream: {stream.StreamId}"))
            {

                _logger.LogInformation("Applying Stream {StreamId} to {TargetType}", stream.StreamId, typeof(TTargetType));
                if (target == null) // we shall construct a new one
                {
                    _logger.LogInformation("Target is null, Creating {TargetType}", typeof(TTargetType));
                    target = new TTargetType();
                }

                foreach (var rawEvent in stream.Stream)
                {
                    // convert the JObject to an IApplyAbleEvent
                    if (rawEvent.Data.ToObject(rawEvent.DataType) is IApplyableEvent<TTargetType> appliableEvent)
                    {
                        _logger.LogDebug("Applying {EventId} of {EventType} with {Version}", rawEvent.Id, rawEvent.DataType, rawEvent.Version);
                        appliableEvent.Apply(target);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to Apply {EventId} of {EventType} with {Version}: Type does not Implement {Interface}",
                            rawEvent.Id,
                            rawEvent.DataType,
                            rawEvent.Version,
                            nameof(IApplyableEvent<TTargetType>)
                        );
                    }
                }
            }
            return target;
        }
    }
}

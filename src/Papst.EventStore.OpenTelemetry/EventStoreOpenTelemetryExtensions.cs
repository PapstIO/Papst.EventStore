using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Papst.EventStore.OpenTelemetry;

/// <summary>
/// Extension methods for adding OpenTelemetry instrumentation to Papst.EventStore.
/// </summary>
public static class EventStoreOpenTelemetryExtensions
{
  /// <summary>
  /// Wraps the registered <see cref="IEventStore"/> with an instrumented decorator that produces
  /// OpenTelemetry-compatible <see cref="System.Diagnostics.Activity"/> instances for all EventStore operations.
  /// <para>
  /// Call this method after registering the EventStore implementation (e.g. after <c>AddInMemoryEventStore()</c>).
  /// To collect these traces configure your OpenTelemetry SDK to listen to the
  /// <see cref="EventStoreActivitySource.SourceName"/> activity source.
  /// </para>
  /// </summary>
  /// <param name="services">The service collection.</param>
  /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
  /// <exception cref="InvalidOperationException">
  /// Thrown when no <see cref="IEventStore"/> has been registered before calling this method.
  /// </exception>
  public static IServiceCollection AddEventStoreInstrumentation(this IServiceCollection services)
  {
    ServiceDescriptor? descriptor = services.LastOrDefault(d => d.ServiceType == typeof(IEventStore));
    if (descriptor == null)
    {
      throw new InvalidOperationException(
        $"No {nameof(IEventStore)} registration found. Register an EventStore implementation before calling {nameof(AddEventStoreInstrumentation)}.");
    }

    services.Remove(descriptor);

    ServiceDescriptor decorated = ServiceDescriptor.Describe(
      typeof(IEventStore),
      sp =>
      {
        IEventStore inner = (IEventStore)(descriptor.ImplementationInstance
          ?? descriptor.ImplementationFactory?.Invoke(sp)
          ?? ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType!));
        return new InstrumentedEventStore(inner);
      },
      descriptor.Lifetime);

    services.Add(decorated);
    return services;
  }
}

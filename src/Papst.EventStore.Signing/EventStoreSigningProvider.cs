using System;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.Pipeline;

namespace Papst.EventStore.Signing;

/// <summary>
/// Dependency Injection extensions for event signing.
/// </summary>
public static class EventStoreSigningProvider
{
  /// <summary>
  /// Adds event signing to any registered EventStore. Registers the signer, the
  /// signing append-pipeline handler, and the stream signature verifier. Call after
  /// the store's own <c>Add*EventStore</c>.
  /// </summary>
  /// <param name="services"></param>
  /// <param name="configure">Configures the signing certificate and algorithm.</param>
  public static IServiceCollection AddEventStoreSigning(
    this IServiceCollection services,
    Action<EventSigningOptions> configure)
  {
    services.Configure(configure);
    services.AddSingleton<IEventSigner, X509EventSigner>();
    services.AddSingleton<IEventStreamSignatureVerifier, X509SignatureVerifier>();
    services.AddAppendPipelineHandler<SigningPipelineHandler>();
    return services;
  }
}

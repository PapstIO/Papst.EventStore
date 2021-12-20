using Microsoft.Azure.Cosmos;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.CosmosDb;

/// <summary>
/// Internal Interface for Cosmos Client abstraction
/// </summary>
internal interface IEventStoreCosmosClient
{
  /// <summary>
  /// Name of the collection
  /// </summary>
  string Collection { get; }

  /// <summary>
  /// Initialization Option during startup
  /// </summary>
  bool InitializeOnStartup { get; }

  /// <summary>
  /// Whether to allow or reject time updates during event creation
  /// </summary>
  bool AllowTimeOverride { get; }

  /// <summary>
  /// Get the Cosmos Clients Container instance
  /// </summary>
  /// <returns></returns>
  Container GetContainer();

  /// <summary>
  /// Initialize the Database
  /// </summary>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task InitializeAsync(CancellationToken cancellationToken);
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Papst.EventStore.FileSystem;
public static class FileSystemEventStoreProvider
{
  /// <summary>
  /// Adds the File System based Event Stream
  /// NOTE: this should only be used for demonstration purposes
  /// </summary>
  /// <param name="services">The Service Collection</param>
  /// <param name="config">The Configuration Section that contains the Path</param>
  /// <returns></returns>
  public static IServiceCollection AddFileSystemEventStore(this IServiceCollection services, IConfiguration config) => services
    .AddTransient<IEventStore, FileSystemEventStore>()
    .Configure<FileSystemEventStoreOptions>(c => config.Bind(c))

    ;
}

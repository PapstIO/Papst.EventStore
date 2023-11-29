using Microsoft.Extensions.Logging;
using Papst.EventStore.Documents;
using Papst.EventStore.Exceptions;
using Papst.EventStore.FileSystem.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.FileSystem;
internal sealed class FileSystemEventStream : IEventStream
{
  private const string FileNameFormat = "000000000000";

  private readonly ILogger<FileSystemEventStream> _logger;
  private readonly string _path;
  private FileSystemStreamIndexEntity _entity;
  private readonly IEventTypeProvider _eventTypeProvider;

  public Guid StreamId => _entity.StreamId;

  public ulong Version => _entity.Version;

  public DateTimeOffset Created => _entity.Created;

  public FileSystemEventStream(ILogger<FileSystemEventStream> logger, string path, FileSystemStreamIndexEntity entity, IEventTypeProvider eventTypeProvider)
  {
    _logger = logger;
    _path = path;
    _entity = entity;
    _eventTypeProvider = eventTypeProvider;
  }

  /// <inheritdoc/>
  public async Task AppendAsync<TEvent>(Guid id, TEvent evt, EventStreamMetaData? metaData = null, CancellationToken cancellationToken = default) where TEvent : notnull
  {
    string eventName = _eventTypeProvider.ResolveType(typeof(TEvent));
    EventStreamDocument document = EventStreamDocument.Create(
      StreamId,
      id,
      _entity.NextVersion,
      eventName,
      evt,
      eventName,
      _entity.TargetType,
      metaData);

    await AppendInternalAsync(document, cancellationToken).ConfigureAwait(false);
    await UpdateIndexAsync().ConfigureAwait(false);
  }

  /// <inheritdoc/>
  public Task<IEventStoreTransactionAppender> CreateTransactionalBatchAsync()
  {
    throw new NotImplementedException();
  }

  /// <inheritdoc/>
  public async Task<EventStreamDocument?> GetLatestSnapshot(CancellationToken cancellationToken = default)
  {
    if (!_entity.LatestSnapshotVersion.HasValue)
    {
      return null;
    }

    string fileName = $"{_entity.LatestSnapshotVersion.Value.ToString(FileNameFormat)}.json";
    if (!File.Exists(Path.Combine(_path, fileName)))
    {
      throw new EventStreamVersionNotFoundException(StreamId, _entity.LatestSnapshotVersion.Value, "The Version File does not exist!");
    }

    using var stream = File.OpenRead(Path.Combine(_path, fileName));
    EventStreamDocument? entity = await JsonSerializer.DeserializeAsync<EventStreamDocument>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    if (entity == null)
    {
      throw new EventStreamVersionNotFoundException(StreamId, _entity.LatestSnapshotVersion.Value, "The Version is not readable!");
    }

    return entity;
  }

  /// <inheritdoc/>
  public IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion= 0u, CancellationToken cancellationToken = default)
    => ListAsync(startVersion, _entity.Version, cancellationToken);

  /// <inheritdoc/>
  public async IAsyncEnumerable<EventStreamDocument> ListAsync(ulong startVersion, ulong endVersion, [EnumeratorCancellation] CancellationToken cancellationToken = default)
  {
    Logging.ReadingEventStream(_logger, StreamId, startVersion, endVersion);
    ulong currentVersion = startVersion;
    while (!cancellationToken.IsCancellationRequested && currentVersion <= endVersion)
    {
      string versionPath = Path.Combine(_path, VersionToPath(currentVersion));
      string fileName = $"{currentVersion.ToString(FileNameFormat)}.json";
      if (!File.Exists(Path.Combine(versionPath, fileName)))
      {
        throw new EventStreamVersionNotFoundException(StreamId, currentVersion, "The Version File does not exist!");
      }
      Logging.ReadingEvent(_logger, StreamId, currentVersion);
      await using var stream = File.OpenRead(Path.Combine(versionPath, fileName));
      EventStreamDocument? entity = await JsonSerializer.
        DeserializeAsync<EventStreamDocument>(stream, cancellationToken: cancellationToken)
        .ConfigureAwait(false);
      if (entity == null)
      {
        throw new EventStreamVersionNotFoundException(StreamId, currentVersion, "The Version is not readable!");
      }
      yield return entity;
      currentVersion++;
    }
  }



  private async Task AppendInternalAsync(EventStreamDocument document, CancellationToken cancellationToken = default)
  {
    string targetPath = Path.Combine(_path, VersionToPath(document.Version));
    Directory.CreateDirectory(targetPath);
    string fileName = Path.Combine(targetPath, $"{document.Version.ToString(FileNameFormat)}.json");
    if (File.Exists(fileName))
    {
      throw new EventStreamVersionMismatchException(document.StreamId, "Version already exists!");
    }
    _entity = _entity with
    {
      Updated = document.Time,
      Version = document.Version,
      NextVersion = document.Version + 1,
    };
    Logging.AppendingEvent(_logger, document.DataType, document.StreamId, document.Version);
    await File.WriteAllTextAsync(fileName, JsonSerializer.Serialize(document), cancellationToken);
  }

  private static string VersionToPath(ulong version) => (version / 100).ToString("0000000000");

  private async Task UpdateIndexAsync()
    => await File.WriteAllTextAsync(
      Path.Combine(_path, FileSystemEventStore.IndexFileName),
      JsonSerializer.Serialize(_entity)).ConfigureAwait(false);
}

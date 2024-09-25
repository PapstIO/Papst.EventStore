using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Papst.EventStore.Exceptions;
using Papst.EventStore.FileSystem.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Papst.EventStore.Documents;

namespace Papst.EventStore.FileSystem;

internal class FileSystemEventStore : IEventStore
{
  internal const string IndexFileName = "_index.json";
  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<FileSystemEventStore> _logger;
  private readonly string _path;
  private readonly IEventTypeProvider _eventTypeProvider;

  public FileSystemEventStore(ILogger<FileSystemEventStore> logger, IOptions<FileSystemEventStoreOptions> options, ILoggerFactory loggerFactory, IEventTypeProvider eventTypeProvider)
  {
    _loggerFactory = loggerFactory;
    _logger = logger;
    _path = options.Value.Path;
    _eventTypeProvider = eventTypeProvider;

    Directory.CreateDirectory(_path);
  }

  public async Task<IEventStream> CreateAsync(Guid streamId, string targetTypeName, CancellationToken cancellationToken = default) => await CreateAsync(
      streamId,
      targetTypeName,
      null,
      null,
      null,
      null,
      null,
      cancellationToken).ConfigureAwait(false);

  public async Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName, 
    string? tenantId,
    string? userId,
    string? username,
    string? comment,
    Dictionary<string, string>? additionalMetaData,
    CancellationToken cancellationToken = default)
  {
    string streamPath = Path.Combine(_path, streamId.ToString());
    if (Directory.Exists(streamPath))
    {
      throw new EventStreamAlreadyExistsException(streamId, "The Event Stream already exists!");
    }
    Logging.CreatingEventStream(_logger, streamId, targetTypeName);

    Directory.CreateDirectory(streamPath);

    FileSystemStreamIndexEntity entity = new(streamId,
      DateTimeOffset.Now,
      0,
      0,
      DateTimeOffset.Now,
      targetTypeName,
      null,
      new EventStreamMetaData()
      {
        TenantId = tenantId,
        UserId = userId,
        UserName = username,
        Comment = comment,
        Additional = additionalMetaData,
      });
    await File.WriteAllTextAsync(Path.Combine(streamPath, IndexFileName), JsonSerializer.Serialize(entity), cancellationToken).ConfigureAwait(false);

    IEventStream stream = new FileSystemEventStream(_loggerFactory.CreateLogger<FileSystemEventStream>(), streamPath, entity, _eventTypeProvider);
    return stream;
  }

  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    Logging.GetEventStream(_logger, streamId);
    string streamPath = Path.Combine(_path, streamId.ToString());
    if (!Directory.Exists(streamPath))
    {
      throw new EventStreamNotFoundException(streamId, "Event Stream Directory not found");
    }

    string indexFile = Path.Combine(streamPath, IndexFileName);
    if (!File.Exists(indexFile))
    {
      throw new EventStreamNotFoundException(streamId, "Event Stream Index File not found");
    }

    FileSystemStreamIndexEntity? entity = JsonSerializer.Deserialize<FileSystemStreamIndexEntity>(await File.ReadAllTextAsync(indexFile).ConfigureAwait(false));
    if (entity == null)
    {
      throw new EventStreamNotFoundException(streamId, "Index File not readable");
    }

    return new FileSystemEventStream(_loggerFactory.CreateLogger<FileSystemEventStream>(), streamPath, entity,
      _eventTypeProvider);
  }
}

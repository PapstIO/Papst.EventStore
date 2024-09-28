using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Papst.EventStore.EntityFrameworkCore.Database;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.EntityFrameworkCore;
public sealed class EntityFrameworkEventStore : IEventStore
{
  private readonly ILogger<EntityFrameworkEventStore> _logger;
  private readonly ILoggerFactory _loggerFactory;
  private readonly IEventTypeProvider _eventTypeProvider;
  private readonly EventStoreDbContext _dbContext;

  public EntityFrameworkEventStore(ILogger<EntityFrameworkEventStore> logger, ILoggerFactory loggerFactory, IEventTypeProvider eventTypeProvider, EventStoreDbContext dbContext)
  {
    _logger = logger;
    _loggerFactory = loggerFactory;
    _eventTypeProvider = eventTypeProvider;
    _dbContext = dbContext;
  }
  public async Task<IEventStream> CreateAsync(Guid streamId, string targetTypeName, CancellationToken cancellationToken = default) =>
    await CreateAsync(streamId,
        targetTypeName,
        null,
        null,
        null,
        null,
        null,
        cancellationToken)
      .ConfigureAwait(false);

  public async Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    string? tenantId,
    string? userId,
    string? username,
    string? comment, Dictionary<string, string>? additionalMetaData,
    CancellationToken cancellationToken = default)
  {
    Logging.CreatingEventStream(_logger, streamId, targetTypeName);
    EventStreamEntity stream = new()
    {
      StreamId = streamId,
      Created = DateTimeOffset.Now,
      Version = 0,
      NextVersion = 0,
      TargetType = targetTypeName,
      Updated = DateTimeOffset.Now,
      MetaDataTenantId = tenantId,
      MetaDataUserId = userId,
      MetaDataUserName = username,
      MetaDataComment = comment,
      MetaDataAdditionJson = JsonSerializer.Serialize(additionalMetaData ?? new Dictionary<string, string>())
    };

    await _dbContext.Streams.AddAsync(stream, cancellationToken).ConfigureAwait(false);
    await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

    stream = await _dbContext.Streams.FirstAsync(s => s.StreamId == streamId, cancellationToken)
      .ConfigureAwait(false);

    return new EntityFrameworkEventStream(_loggerFactory.CreateLogger<EntityFrameworkEventStream>(), _dbContext, stream, _eventTypeProvider);
  }


  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    Logging.GetEventStream(_logger, streamId);

    EventStreamEntity? stream = await _dbContext.Streams.FirstOrDefaultAsync(s => s.StreamId == streamId, cancellationToken)
      .ConfigureAwait(false);
    if (stream == null)
    {
      throw new EventStreamNotFoundException(streamId, "Event Stream Index not found!");
    }

    return new EntityFrameworkEventStream(
      _loggerFactory.CreateLogger<EntityFrameworkEventStream>(),
      _dbContext,
      stream,
      _eventTypeProvider);
  }
}

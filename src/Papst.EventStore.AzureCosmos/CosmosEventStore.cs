using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Documents;
using Papst.EventStore.Exceptions;
using System.Net;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStore(
  ILogger<CosmosEventStore> logger,
  ILoggerFactory loggerFactory,
  IOptions<CosmosEventStoreOptions> options,
  CosmosDatabaseProvider dbProvider,
  IEventTypeProvider eventTypeProvider,
  ICosmosIdStrategy idStrategy,
  TimeProvider timeProvider
)
  : IEventStore
{
  internal const string IndexDocumentName = "Index";

  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    logger.GetEventStream(streamId);
    ItemResponse<EventStreamIndexEntity>? stream;
    try
    {
      stream = await dbProvider.Container
        .ReadItemAsync<EventStreamIndexEntity>(
          IndexDocumentName,
          new(streamId.ToString()),
          cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
    {
      if (options.Value.BuildIndexOnNotFound)
      {
        stream = await ReadStreamAndBuildIndex(streamId, cancellationToken).ConfigureAwait(false);
      }
      else
      {
        throw new EventStreamNotFoundException(streamId, "Event Stream has not been found!", e);
      }
    }

    return new CosmosEventStream(
      loggerFactory.CreateLogger<CosmosEventStream>(),
      options.Value,
      stream.Resource,
      dbProvider,
      eventTypeProvider,
      idStrategy,
      timeProvider);
  }

  private async Task<ItemResponse<EventStreamIndexEntity>> ReadStreamAndBuildIndex(
    Guid streamId,
    CancellationToken cancellationToken
  )
  {
    // read the stream latest versions
    QueryDefinition query = new QueryDefinition(
      @"SELECT * FROM c
      WHERE c.StreamId = @streamId
      AND (
        c.Version = (SELECT VALUE MAX(c2.Version) FROM c c2 WHERE c2.StreamId = @streamId)
        OR c.Version = 0
        OR c.Version = (SELECT VALUE MAX(c3.Version) FROM c c3 WHERE c3.StreamId = @streamId AND c.DocumentType == 'Snapshot')
      )"
    ).WithParameter("@streamId", streamId.ToString());

    var documents = await dbProvider.Container.GetItemQueryIterator<EventStreamDocumentEntity>(query)
      .ReadNextAsync(cancellationToken)
      .ConfigureAwait(false);

    if (documents.Count < 2)
    {
      throw new EventStreamNotFoundException(streamId, "The Stream has not been found when trying to built the index");
    }

    var creationDocument = documents.First(x => x.Version == 0);
    var maxVersionDocument = documents.First(x => x.Version != 0);

    string? tenantId = null;
    // when option is set and the latest document has a tenant id, use it here
    if (options.Value.UpdateTenantIdOnAppend && maxVersionDocument.MetaData?.TenantId is not null)
    {
      tenantId = maxVersionDocument.MetaData.TenantId;
    }
    // if tenant id option is not set, or latest event does not have a tenant id, try to use the first one
    else if (creationDocument.MetaData?.TenantId is not null)
    {
      tenantId = creationDocument.MetaData.TenantId; 
    }
    
    EventStreamIndexEntity index = new()
    {
      StreamId = streamId,
      Created = creationDocument.Time,
      Version = maxVersionDocument.Version,
      NextVersion = maxVersionDocument.Version + 1,
      Updated = maxVersionDocument.Time,
      TargetType = creationDocument.TargetType,
      LatestSnapshotVersion = documents.FirstOrDefault(x => x.DocumentType == EventStreamDocumentType.Snapshot)?.Version,
      MetaData = new()
      {
        TenantId = tenantId,
      },
    };

    try
    {
      return await dbProvider
        .Container
        .CreateItemAsync(index, new PartitionKey(streamId.ToString()), cancellationToken: cancellationToken)
        .ConfigureAwait(false);
    }
    catch (CosmosException e2) when (e2.StatusCode == HttpStatusCode.Conflict)
    {
      // when already exists reread it from the db
      return await dbProvider.Container
        .ReadItemAsync<EventStreamIndexEntity>(
          IndexDocumentName,
          new(streamId.ToString()),
          cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    catch (CosmosException e)
    {
      throw new EventStreamAlreadyExistsException(streamId, "Event Stream Index already exception when trying to built the index", e);
    }
  }

  public async Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    CancellationToken cancellationToken = default
  ) =>
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
    string? comment,
    Dictionary<string, string>? additionalMetaData,
    CancellationToken cancellationToken = default)
  {
    logger.CreatingEventStream(streamId, targetTypeName);

    EventStreamIndexEntity stream = new()
    {
      StreamId = streamId,
      TargetType = targetTypeName,
      Updated = DateTimeOffset.Now,
      Version = 0,
      NextVersion = 0,
      Created = DateTimeOffset.Now,
      ETag = string.Empty,
      LatestSnapshotVersion = null,
      MetaData = new EventStreamMetaData()
      {
        UserId = userId,
        UserName = username,
        TenantId = tenantId,
        Comment = comment,
        Additional = additionalMetaData
      },
    };

    ItemResponse<EventStreamIndexEntity>? response = await dbProvider.Container.CreateItemAsync(
      stream,
      new PartitionKey(streamId.ToString()),
      cancellationToken: cancellationToken).ConfigureAwait(false);

    return new CosmosEventStream(
      loggerFactory.CreateLogger<CosmosEventStream>(),
      options.Value,
      response.Resource,
      dbProvider,
      eventTypeProvider,
      idStrategy,
      timeProvider);
  }
}

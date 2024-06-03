﻿using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Papst.EventStore.AzureCosmos.Database;
using Papst.EventStore.Documents;
using Papst.EventStore.Exceptions;

namespace Papst.EventStore.AzureCosmos;

internal sealed class CosmosEventStore(
  ILogger<CosmosEventStore> logger,
  ILoggerFactory loggerFactory,
  IOptions<CosmosEventStoreOptions> options,
  CosmosDatabaseProvider dbProvider,
  IEventTypeProvider eventTypeProvider,
  ICosmosIdStrategy idStrategy
)
  : IEventStore
{
  internal const string IndexDocumentName = "Index";

  public async Task<IEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default)
  {
    Logging.GetEventStream(logger, streamId);
    ItemResponse<EventStreamIndexEntity>? stream;
    try
    {
      stream = await dbProvider.Container
        .ReadItemAsync<EventStreamIndexEntity>(
          IndexDocumentName,
          new(streamId.ToString()),
          cancellationToken: cancellationToken).ConfigureAwait(false);
    }
    catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound && options.Value.BuildIndexOnNotFound)
    {
      stream = await ReadStreamAndBuildIndex(streamId, cancellationToken).ConfigureAwait(false);
    }

    return new CosmosEventStream(
      loggerFactory.CreateLogger<CosmosEventStream>(),
      options.Value,
      stream.Resource,
      dbProvider,
      eventTypeProvider,
      idStrategy);
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
    ).WithParameter("streamId", streamId.ToString());
    
    var documents = await dbProvider.Container.GetItemQueryIterator<EventStreamDocumentEntity>(query)
      .ReadNextAsync(cancellationToken)
      .ConfigureAwait(false);

    if (documents.Count < 2)
    {
      throw new EventStreamNotFoundException(streamId, "The Stream has not been found when trying to built the index");
    }
    
    var creationDocument = documents.First(x => x.Version == 0);
    var maxVersionDocument = documents.First(x => x.Version != 0);

    EventStreamIndexEntity index = new()
    {
      StreamId = streamId,
      Created = creationDocument.Time,
      Version = maxVersionDocument.Version,
      NextVersion = maxVersionDocument.Version + 1,
      Updated = maxVersionDocument.Time,
      TargetType = creationDocument.TargetType,
      LatestSnapshotVersion = documents.FirstOrDefault(x => x.DocumentType == EventStreamDocumentType.Snapshot)?.Version
    };

    try
    {
      return await dbProvider
        .Container
        .CreateItemAsync(index, new PartitionKey(streamId.ToString()), cancellationToken: cancellationToken)
        .ConfigureAwait(false);
    }
    catch (Exception e)
    {
      throw new EventStreamAlreadyExistsException(streamId, "Event Stream Index already existed when trying to built the index", e);
    }
  }

  public async Task<IEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    CancellationToken cancellationToken = default
  )
  {
    Logging.CreatingEventStream(logger, streamId, targetTypeName);

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
      idStrategy);
  }
}

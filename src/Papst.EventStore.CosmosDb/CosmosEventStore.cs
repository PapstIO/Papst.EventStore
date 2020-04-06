using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Papst.EventStore.Abstractions;
using Papst.EventStore.Abstractions.Exceptions;
using Papst.EventStore.CosmosDb.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.CosmosDb
{
    /// <summary>
    /// Cosmos DB Implementation for <see cref="IEventStore"/>
    /// </summary>
    /// <inheritdoc />
    class CosmosEventStore : IEventStore
    {
        private readonly ILogger<CosmosEventStore> _logger;
        private readonly EventStoreCosmosClient _client;

        public CosmosEventStore(EventStoreCosmosClient client, ILogger<CosmosEventStore> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _client = client;
        }

        /// <inheritdoc />
        public async Task<EventStoreResult> AppendAsync(Guid streamId, ulong expectedVersion, EventStreamDocument doc, CancellationToken token = default)
        {
            Container container = await InitAsync(token).ConfigureAwait(false);

            EventStreamDocumentEntity lastStreamDoc = await container.ReadItemAsync<EventStreamDocumentEntity>(
                $"{streamId}|{expectedVersion}",
                new PartitionKey(streamId.ToString()),
                cancellationToken: token
            ).ConfigureAwait(false);

            if (lastStreamDoc == null)
            {
                throw new EventStreamNotFoundException(streamId, "Stream does not exist");
            }

            EventStreamDocumentEntity documentEntity = PrepareDocument(lastStreamDoc, doc);

            var result = await container.CreateItemAsync(documentEntity, new PartitionKey(lastStreamDoc.StreamId.ToString()), cancellationToken: token).ConfigureAwait(false);

            if (result.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Inserted {Document} with {Version} to {Stream}", result.Resource.DocumentId, result.Resource.Version, result.Resource.StreamId);
                return new EventStoreResult
                {
                    DocumentId = result.Resource.DocumentId,
                    StreamId = streamId,
                    Version = result.Resource.Version,
                    IsSuccess = true,
                };
            }
            else if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    "Inserting {Documet} with {Version} to {Stream} failed because of Version Conflict",
                    documentEntity.DocumentId,
                    documentEntity.Version,
                    streamId
                );
                throw new EventStreamVersionMismatchException(streamId, "A Document with that Version already exists");
            }
            else
            {
                _logger.LogError(
                    "Inserted {Document} with {Version} to {Stream} failed with {Reason}",
                    documentEntity.DocumentId,
                    documentEntity.Version,
                    streamId,
                    result.Diagnostics);

                return new EventStoreResult
                {
                    DocumentId = null,
                    StreamId = streamId,
                    Version = lastStreamDoc.Version,
                    IsSuccess = false
                };
            }
        }

        public async Task<EventStoreResult> AppendAsync(Guid streamId, ulong expectedVersion, IEnumerable<EventStreamDocument> documents, CancellationToken token = default)
        {
            if (!documents.Any())
            {
                throw new NotSupportedException("Document Collection must not be empty");
            }

            Container container = await InitAsync(token).ConfigureAwait(false);

            EventStreamDocumentEntity lastStreamDoc = await container.ReadItemAsync<EventStreamDocumentEntity>(
                $"{streamId}|{expectedVersion}",
                new PartitionKey(streamId.ToString()),
                cancellationToken: token
            ).ConfigureAwait(false);

            if (lastStreamDoc == null)
            {
                throw new EventStreamNotFoundException(streamId, "Stream does not exist");
            }

            var transaction = container.CreateTransactionalBatch(new PartitionKey(streamId.ToString()));

            var lastItem = lastStreamDoc;

            foreach (EventStreamDocument doc in documents)
            {
                lastItem = PrepareDocument(lastItem, doc);
                transaction.CreateItem(lastItem);
            }
            var result = await transaction.ExecuteAsync(token).ConfigureAwait(false);

            if (result.Any(x => x.StatusCode != System.Net.HttpStatusCode.Created))
            {
                _logger.LogError(
                    "Inserting Multiple Documents to {Stream} failed with {Reason}",
                    streamId,
                    result.Diagnostics);

                return new EventStoreResult
                {
                    DocumentId = null,
                    StreamId = streamId,
                    IsSuccess = false
                };
            }
            else
            {
                _logger.LogInformation("Inserted Documents to {Stream} with new {Version}",
                    streamId,
                    lastItem.Version
                );

                return new EventStoreResult
                {
                    DocumentId = null,
                    StreamId = streamId,
                    IsSuccess = true
                };
            }
        }

        /// <inheritdoc />
        public async Task<EventStoreResult> AppendSnapshotAsync(Guid streamId, ulong expectedVersion, EventStreamDocument snapshot, bool deleteOlderSnapshots = true, CancellationToken token = default)
        {
            if (snapshot.DocumentType != EventStreamDocumentType.Snapshot)
            {
                throw new NotSupportedException("Snapshot Document must be of type Snapshot");
            }

            Container container = await InitAsync(token).ConfigureAwait(false);

            EventStreamDocumentEntity lastStreamDoc = await container.ReadItemAsync<EventStreamDocumentEntity>(
                $"{streamId}|{expectedVersion}",
                new PartitionKey(streamId.ToString()),
                cancellationToken: token
            ).ConfigureAwait(false);

            if (lastStreamDoc == null)
            {
                throw new EventStreamNotFoundException(streamId, "Stream does not exist");
            }

            EventStreamDocumentEntity documentEntity = PrepareDocument(lastStreamDoc, snapshot);


            var result = await container.CreateItemAsync(documentEntity, new PartitionKey(lastStreamDoc.StreamId.ToString()), cancellationToken: token).ConfigureAwait(false);

            if (result.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Inserted {Document} with {Version} to {Stream}", result.Resource.DocumentId, result.Resource.Version, result.Resource.StreamId);
                return new EventStoreResult
                {
                    DocumentId = result.Resource.DocumentId,
                    StreamId = streamId,
                    Version = result.Resource.Version,
                    IsSuccess = true,
                };
            }
            else if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogWarning(
                    "Inserting {Documet} with {Version} to {Stream} failed because of Version Conflict",
                    documentEntity.DocumentId,
                    documentEntity.Version,
                    streamId
                );
                throw new EventStreamVersionMismatchException(streamId, "A Document with that Version already exists");
            }
            else
            {
                _logger.LogError(
                    "Inserted {Document} with {Version} to {Stream} failed with {Reason}",
                    documentEntity.DocumentId,
                    documentEntity.Version,
                    streamId,
                    result.Diagnostics);

                return new EventStoreResult
                {
                    DocumentId = null,
                    StreamId = streamId,
                    IsSuccess = false
                };
            }
        }

        /// <inheritdoc />
        public async Task<IEventStream> CreateAsync(Guid streamId, EventStreamDocument doc, CancellationToken token = default)
        {
            if (doc.DocumentType != EventStreamDocumentType.Header)
            {
                throw new NotSupportedException("First Document must be of Type Header");
            }

            Container container = await InitAsync(token).ConfigureAwait(false);

            // try to get Stream
            EventStreamDocumentEntity lastStreamDoc = await container.ReadItemAsync<EventStreamDocumentEntity>(
                GetDocumentId(streamId, EventStreamDocumentType.Header, 0),
                new PartitionKey(streamId.ToString()),
                cancellationToken: token
            ).ConfigureAwait(false);

            if (lastStreamDoc != null)
            {
                _logger.LogWarning("Stream {Stream} already exists!", streamId);
                throw new EventStreamAlreadyExistsException(streamId, "Stream already exists!");
            }
            var documentEntity = Map(doc);

            var result = await container.CreateItemAsync(documentEntity, new PartitionKey(streamId.ToString()), cancellationToken: token);

            if (result.StatusCode == System.Net.HttpStatusCode.Created)
            {
                _logger.LogInformation("Created {Stream}", streamId);
                return new CosmosEventStream(new [] { Map(result.Resource) });
            }
            else if (result.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogError("Creating Stream {Stream} failed with Conflict", streamId);
                throw new EventStreamAlreadyExistsException(streamId, "Stream aldready exists!");
            }
            else
            {
                _logger.LogError("Failed to Create {Stream} with {Reason}", streamId, result.Diagnostics);
                throw new EventStreamException(streamId, "Failed to Create Stream");
            }
        }

        /// <inheritdoc />
        public async Task<IEventStream> ReadAsync(Guid streamId, ulong fromVersion, CancellationToken token = default)
        {
            Container container = await InitAsync(token).ConfigureAwait(false);

            QueryDefinition query = new QueryDefinition($"SELECT * FROM {container.Id} d WHERE d.StreamId = @streamId AND d.Version >= @version ORDER BY d.Version ASC");
            query.WithParameter("@streamId", streamId)
                .WithParameter("@version", fromVersion);
            string continuationToken = null;

            var iterator = container.GetItemQueryIterator<EventStreamDocumentEntity>(query, continuationToken);

            List<EventStreamDocument> documents = new List<EventStreamDocument>();

            while (iterator.HasMoreResults)
            {
                var result = await iterator.ReadNextAsync(token).ConfigureAwait(false);

                documents.AddRange(result.Select(Map));
            }

            return new CosmosEventStream(documents);
        }

        /// <inheritdoc />
        public Task<IEventStream> ReadAsync(Guid streamId, CancellationToken token = default)
        {
            return ReadAsync(streamId, 0, token);
        }

        /// <inheritdoc />
        public async Task<IEventStream> ReadFromSnapshotAsync(Guid streamId, CancellationToken token = default)
        {
            Container container = await InitAsync(token).ConfigureAwait(false);

            QueryDefinition query = new QueryDefinition($"SELECT * FROM {container.Id} d WHERE d.StreamId = @streamId AND (d.DocumentType = @snaphotType OR d.DocumentType = @headerType) ORDER BY d.Version DESC OFFSET 0 LIMIT 1");
            query.WithParameter("@streamId", streamId)
                .WithParameter("@snapshotType", EventStreamDocumentType.Snapshot.ToString())
                .WithParameter("@headerType", EventStreamDocumentType.Header.ToString());

            FeedIterator<EventStreamDocumentEntity> iterator = container.GetItemQueryIterator<EventStreamDocumentEntity>(query);
            FeedResponse<EventStreamDocumentEntity> items = await iterator.ReadNextAsync(token).ConfigureAwait(false);

            if (!items.Any())
            {
                throw new EventStreamNotFoundException(streamId, "Stream not found");
            }

            var snapShotOrHeader = items.First();

            return await ReadAsync(streamId, snapShotOrHeader.Version, token).ConfigureAwait(false);

        }

        private async Task<Container> InitAsync(CancellationToken token)
        {
            if (_client.Options.InitializeOnStartup)
            {
                _logger.LogInformation("Initializing Database");

                DatabaseResponse db = await _client.CreateDatabaseIfNotExistsAsync(_client.Options.Database, cancellationToken: token);
                if (db.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    _logger.LogInformation("Created Database {Database} in Cosmos DB", _client.Options.Database);
                }
                ContainerResponse container = await db.Database.CreateContainerIfNotExistsAsync(new ContainerProperties { Id = _client.Options.Collection }, cancellationToken: token);
                if (container.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    _logger.LogInformation("Created Container {Container} in {Database}", _client.Options.Collection, _client.Options.Database);
                }

                return container.Container;
            }
            else
            {
                return _client.GetContainer(_client.Options.Database, _client.Options.Collection);
            }
        }

        private EventStreamDocumentEntity Map(EventStreamDocument doc) => new EventStreamDocumentEntity
        {
            Id = GetDocumentId(doc.StreamId, doc.DocumentType, doc.Version),
            DocumentId = doc.Id,
            StreamId = doc.StreamId,
            DocumentType = doc.DocumentType,
            Version = doc.Version,
            Time = doc.Time,
            Name = doc.Name,
            Data = doc.Data,
            DataType = doc.DataType,
            TargetType = doc.TargetType,
            MetaData = doc.MetaData,
        };

        private EventStreamDocument Map(EventStreamDocumentEntity doc) => new EventStreamDocument
        {
            Id = doc.DocumentId,
            StreamId = doc.StreamId,
            DocumentType = doc.DocumentType,
            Version = doc.Version,
            Time = doc.Time,
            Name = doc.Name,
            Data = doc.Data,
            DataType = doc.DataType,
            TargetType = doc.TargetType,
            MetaData = doc.MetaData,
        };

        private string GetDocumentId(Guid streamId, EventStreamDocumentType documentType, ulong version)
        {
            switch (documentType)
            {
                case EventStreamDocumentType.Event:
                    return streamId.ToString();

                case EventStreamDocumentType.Header:
                    return $"{streamId}|Document|{version}";

                case EventStreamDocumentType.Snapshot:
                    return $"{streamId}|Snap|{version}";

                default:
                    throw new NotSupportedException($"EventStreamDocumentType {documentType} is not Supported!");
            }
        }

        private EventStreamDocumentEntity PrepareDocument(EventStreamDocumentEntity lastStreamDoc, EventStreamDocument doc)
        {
            // prepare the Document
            EventStreamDocumentEntity documentEntity = Map(doc);
            documentEntity.Version = lastStreamDoc.Version + 1;
            documentEntity.StreamId = lastStreamDoc.StreamId;
            if (!_client.Options.AllowTimeOverride)
            {
                documentEntity.Time = DateTimeOffset.Now;
            }

            return documentEntity;
        }
    }
}

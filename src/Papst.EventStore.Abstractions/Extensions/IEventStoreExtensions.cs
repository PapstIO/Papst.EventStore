using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Papst.EventStore.Abstractions.Extensions;

/// <summary>
/// Extensions for the <see cref="IEventStore"/> Interface
/// </summary>
public static class IEventStoreExtensions
{
  /// <summary>
  /// Append Event to Store
  /// </summary>
  /// <typeparam name="TDocument">Type of the Body</typeparam>
  /// <typeparam name="TTargetType">Type of the Target Read Model</typeparam>
  /// <param name="store">The Store where the Event will be appended</param>
  /// <param name="streamId">The Stream Id</param>
  /// <param name="name">Name of the Event</param>
  /// <param name="expectedVersion">Expected Version of the Stream</param>
  /// <param name="document">The Body</param>
  /// <param name="userId">Id of the User who Appended the Document</param>
  /// <param name="username">Name of the User who appended the Document</param>
  /// <param name="tenantId">Tenant that owns the document</param>
  /// <param name="comment">Comment for the Event</param>
  /// <param name="additional">Additional Meta Items</param>
  /// <param name="token">Cancellation Token for the operation</param>
  /// <returns></returns>
  public static Task<EventStoreResult> AppendEventAsync<TDocument, TTargetType>(
      this IEventStore store,
      Guid streamId,
      string name,
      ulong expectedVersion,
      TDocument document,
      Guid? userId = null,
      string? username = null,
      Guid? tenantId = null,
      string? comment = null,
      Dictionary<string, string>? additional = null,
      CancellationToken token = default
  )
      where TDocument : class
  {
    return store.AppendAsync(
        streamId,
        expectedVersion,
        new EventStreamDocument
        {
          Id = Guid.NewGuid(),
          StreamId = streamId,
          DocumentType = EventStreamDocumentType.Event,
          Data = JObject.FromObject(document),
          DataType = typeof(TDocument),
          TargetType = typeof(TTargetType),
          Name = name,
          Time = DateTimeOffset.Now,
          Version = 0,
          MetaData = new EventStreamMetaData
          {
            UserId = userId,
            UserName = username,
            Comment = comment,
            TenantId = tenantId,
            Additional = additional
          }
        },
        token
    );
  }

  /// <summary>
  /// Creates a new Event Stream
  /// </summary>
  /// <typeparam name="TDocument">Type of the Body</typeparam>
  /// <typeparam name="TTargetType">Type of the Target Read Model</typeparam>
  /// <param name="store">The Store where the Event will be appended</param>
  /// <param name="streamId">The Stream Id</param>
  /// <param name="name">Name of the Event</param>
  /// <param name="document">The Body</param>
  /// <param name="userId">Id of the User who Appended the Document</param>
  /// <param name="username">Name of the User who appended the Document</param>
  /// <param name="tenantId">Tenant that owns the document</param>
  /// <param name="comment">Comment for the Event</param>
  /// <param name="additional">Additional Meta Items</param>
  /// <param name="token">Cancellation Token for the operation</param>
  /// <returns></returns>
  public static Task<IEventStream> CreateEventStreamAsync<TDocument, TTargetType>(
      this IEventStore store,
      Guid streamId,
      string name,
      TDocument document,
      Guid? userId = null,
      string? username = null,
      Guid? tenantId = null,
      string? comment = null,
      Dictionary<string, string>? additional = null,
      CancellationToken token = default
  )
      where TDocument : class
  {
    return store.CreateAsync(
        streamId,
        new EventStreamDocument
        {
          Id = Guid.NewGuid(),
          StreamId = streamId,
          DocumentType = EventStreamDocumentType.Event,
          Data = JObject.FromObject(document),
          DataType = typeof(TDocument),
          TargetType = typeof(TTargetType),
          Name = name,
          Time = DateTimeOffset.Now,
          Version = 0,
          MetaData = new EventStreamMetaData
          {
            UserId = userId,
            UserName = username,
            Comment = comment,
            TenantId = tenantId,
            Additional = additional
          }
        },
        token
        );
  }

  /// <summary>
  /// Creates a Snapshot at the end of the current Stream
  /// </summary>
  /// <typeparam name="TTargetType"></typeparam>
  /// <param name="store"></param>
  /// <param name="streamId"></param>
  /// <param name="name"></param>
  /// <param name="expectedVersion"></param>
  /// <param name="entity"></param>
  /// <param name="userId"></param>
  /// <param name="username"></param>
  /// <param name="tenantId"></param>
  /// <param name="comment"></param>
  /// <param name="additional"></param>
  /// <param name="token"></param>
  /// <returns></returns>
  public static Task<EventStoreResult> AppendSnapshotAsync<TTargetType>(
      this IEventStore store,
      Guid streamId,
      string name,
      ulong expectedVersion,
      TTargetType entity,
      Guid? userId = null,
      string? username = null,
      Guid? tenantId = null,
      string? comment = null,
      Dictionary<string, string>? additional = null,
      CancellationToken token = default
  )
      where TTargetType : IEntity
  {
    return store.AppendSnapshotAsync(streamId, expectedVersion, new EventStreamDocument
    {
      Id = Guid.NewGuid(),
      StreamId = streamId,
      DocumentType = EventStreamDocumentType.Snapshot,
      Data = JObject.FromObject(entity),
      DataType = typeof(TTargetType),
      TargetType = typeof(TTargetType),
      Name = name,
      Time = DateTimeOffset.Now,
      Version = 0,
      MetaData = new EventStreamMetaData
      {
        UserId = userId,
        UserName = username,
        Comment = comment,
        TenantId = tenantId,
        Additional = additional
      },
    });
  }
}

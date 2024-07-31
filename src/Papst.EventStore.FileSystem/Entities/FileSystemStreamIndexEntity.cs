using System;

namespace Papst.EventStore.FileSystem.Entities;
internal record FileSystemStreamIndexEntity(
  Guid StreamId,
  DateTimeOffset Created,
  ulong Version,
  ulong NextVersion,
  DateTimeOffset Updated,
  string TargetType,
  ulong? LatestSnapshotVersion);

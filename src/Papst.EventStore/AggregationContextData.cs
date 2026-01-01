namespace Papst.EventStore;

public record AggregationContextData(
  string Key,
  ulong Version,
  ulong? ValidUntilVersion,
  string Value);

namespace Papst.EventStore.Abstractions;

public interface IEntity
{
  ulong Version { get; set; }
}

namespace Papst.EventStore.EventRegistration;

/// <summary>
/// A Collection of Registered Events
/// </summary>
public interface IEventRegistration
{
  /// <summary>
  /// Adds the <typeparamref name="TEvent"/> to the Registration
  /// </summary>
  /// <param name="descriptors"></param>
  /// <typeparam name="TEvent"></typeparam>
  void AddEvent<TEvent>(params EventAttributeDescriptor[] descriptors);

  internal IReadOnlyDictionary<string, Type> ReadEvents { get; }
  internal IReadOnlyDictionary<Type, string> WriteEvents { get; }
}

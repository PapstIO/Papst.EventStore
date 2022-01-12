using System;

namespace Papst.EventStore.Abstractions.EventAggregation;

/// <summary>
/// Utility Class to Provide Type Helpers for backwards compatability
/// </summary>
public static class TypeUtils
{
  /// <summary>
  /// Gets a reverse compatible name of a type
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static string NameOfType(Type type) => $"{type.FullName},{type.Assembly.GetName().Name}";

  /// <summary>
  /// Gets the Type Information for a reverse compatible name
  /// </summary>
  /// <param name="name"></param>
  /// <returns></returns>
  public static Type? TypeOfName(string name) => Type.GetType(name);
}

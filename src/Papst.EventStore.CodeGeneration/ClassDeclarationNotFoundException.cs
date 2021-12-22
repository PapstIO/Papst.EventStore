using System;

namespace Papst.EventStore.CodeGeneration
{

  [Serializable]
  public class ClassDeclarationNotFoundException : Exception
  {
    public ClassDeclarationNotFoundException() { }
    public ClassDeclarationNotFoundException(string message) : base(message) { }
    public ClassDeclarationNotFoundException(string message, Exception inner) : base(message, inner) { }
    protected ClassDeclarationNotFoundException(
    System.Runtime.Serialization.SerializationInfo info,
    System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
  }
}

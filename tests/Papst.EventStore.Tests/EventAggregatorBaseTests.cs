#nullable enable
using System;
using System.Threading.Tasks;
using Papst.EventStore.Aggregation;
using Shouldly;
using Xunit;

namespace Papst.EventStore.Tests;

public class EventAggregatorBaseTests
{
  private readonly TestAggregator _sut = new();

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetter_WhenNullableStructHasValue()
  {
    int result = 0;
    int? value = 42;

    _sut.CallSetIfNotNull(v => result = v, value);

    result.ShouldBe(42);
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetter_WhenNullableStructIsNull()
  {
    int result = 0;
    int? value = null;

    _sut.CallSetIfNotNull(v => result = v, value);

    result.ShouldBe(0);
  }

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetter_WhenNullableDateTimeHasValue()
  {
    DateTime result = default;
    DateTime? value = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    _sut.CallSetIfNotNull(v => result = v, value);

    result.ShouldBe(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetter_WhenNullableDateTimeIsNull()
  {
    DateTime result = default;
    DateTime? value = null;

    _sut.CallSetIfNotNull(v => result = v, value);

    result.ShouldBe(default(DateTime));
  }

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetter_WhenNullableGuidHasValue()
  {
    Guid result = Guid.Empty;
    Guid expected = Guid.NewGuid();
    Guid? value = expected;

    _sut.CallSetIfNotNull(v => result = v, value);

    result.ShouldBe(expected);
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetter_WhenNullableGuidIsNull()
  {
    Guid result = Guid.Empty;
    Guid? value = null;

    _sut.CallSetIfNotNull(v => result = v, value);

    result.Should().Be(Guid.Empty);
  }

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetterForNullable_WhenNullableStructHasValue()
  {
    int? result = 0;
    int? value = 42;

    _sut.CallSetIfNotNull((Action<int?>)(v => result = v), value);

    result.Should().Be(42);
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetterForNullable_WhenNullableStructIsNull()
  {
    int? result = 0;
    int? value = null;

    _sut.CallSetIfNotNull((Action<int?>)(v => result = v), value);

    result.Should().Be(0);
  }

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetterForNullable_WhenNullableDateTimeHasValue()
  {
    DateTime? result = null;
    DateTime? value = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    _sut.CallSetIfNotNull((Action<DateTime?>)(v => result = v), value);

    result.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetterForNullable_WhenNullableDateTimeIsNull()
  {
    DateTime? result = default(DateTime);
    DateTime? value = null;

    _sut.CallSetIfNotNull((Action<DateTime?>)(v => result = v), value);

    result.Should().Be(default(DateTime));
  }

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetterForNullable_WhenNullableGuidHasValue()
  {
    Guid? result = Guid.Empty;
    Guid expected = new Guid();
    Guid? value = expected;

    _sut.CallSetIfNotNull((Action<Guid?>)(v => result = v), value);

    result.Should().Be(expected);
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetterForNullable_WhenNullableGuidIsNull()
  {
    Guid? result = Guid.Empty;
    Guid? value = null;

    _sut.CallSetIfNotNull((Action<Guid?>)(v => result = v), value);

    result.ShouldBe(Guid.Empty);
  }

  [Fact]
  public void Update_ShouldInvokeSetter_WhenNullableStructHasValue()
  {
    int result = 0;
    int? value = 42;

    _sut.CallUpdate(value, v => result = v);

    result.ShouldBe(42);
  }

  [Fact]
  public void Update_ShouldNotInvokeSetter_WhenNullableStructIsNull()
  {
    int result = 0;
    int? value = null;

    _sut.CallUpdate(value, v => result = v);

    result.ShouldBe(0);
  }

  [Fact]
  public void Update_ShouldInvokeSetter_WhenNullableDateTimeHasValue()
  {
    DateTime result = default;
    DateTime? value = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    _sut.CallUpdate(value, v => result = v);

    result.ShouldBe(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void Update_ShouldNotInvokeSetter_WhenNullableDateTimeIsNull()
  {
    DateTime result = default;
    DateTime? value = null;

    _sut.CallUpdate(value, v => result = v);

    result.ShouldBe(default(DateTime));
  }

  [Fact]
  public void Update_ShouldInvokeSetter_WhenNullableGuidHasValue()
  {
    Guid result = Guid.Empty;
    Guid expected = Guid.NewGuid();
    Guid? value = expected;

    _sut.CallUpdate(value, v => result = v);

    result.ShouldBe(expected);
  }

  [Fact]
  public void Update_ShouldNotInvokeSetter_WhenNullableGuidIsNull()
  {
    Guid result = Guid.Empty;
    Guid? value = null;

    _sut.CallUpdate(value, v => result = v);

    result.Should().Be(Guid.Empty);
  }

  [Fact]
  public void Update_ShouldInvokeSetterForNullable_WhenNullableStructHasValue()
  {
    int? result = 0;
    int? value = 42;

    _sut.CallUpdate(value,(Action<int?>)(v => result = v));

    result.Should().Be(42);
  }

  [Fact]
  public void Update_ShouldNotInvokeSetterForNullable_WhenNullableStructIsNull()
  {
    int? result = 0;
    int? value = null;

    _sut.CallUpdate(value, (Action<int?>)(v => result = v));

    result.Should().Be(0);
  }

  [Fact]
  public void Update_ShouldInvokeSetterForNullable_WhenNullableDateTimeHasValue()
  {
    DateTime? result = null;
    DateTime? value = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    _sut.CallUpdate(value, (Action<DateTime?>)(v => result = v));

    result.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void Update_ShouldNotInvokeSetterForNullable_WhenNullableDateTimeIsNull()
  {
    DateTime? result = default(DateTime);
    DateTime? value = null;

    _sut.CallUpdate(value, (Action<DateTime?>)(v => result = v));

    result.Should().Be(default(DateTime));
  }

  [Fact]
  public void Update_ShouldInvokeSetterForNullable_WhenNullableGuidHasValue()
  {
    Guid? result = Guid.Empty;
    Guid expected = Guid.NewGuid();
    Guid? value = expected;

    _sut.CallUpdate(value, (Action<Guid?>)(v => result = v));

    result.Should().Be(expected);
  }

  [Fact]
  public void Update_ShouldNotInvokeSetterForNullable_WhenNullableGuidIsNull()
  {
    Guid? result = Guid.Empty;
    Guid? value = null;

    _sut.CallUpdate(value, (Action<Guid?>)(v => result = v));

    result.ShouldBe(Guid.Empty);
  }

  private class TestEntity
  {
    public int IntValue { get; set; }
  }

  private record TestEvent;

  private class TestAggregator : EventAggregatorBase<TestEntity, TestEvent>
  {
    public override ValueTask<TestEntity?> ApplyAsync(TestEvent evt, TestEntity entity, IAggregatorStreamContext ctx)
      => AsTask(entity);

    public void CallSetIfNotNull<T>(Action<T> setter, T? value) where T : struct
      => SetIfNotNull(setter, value);
    
    public void CallSetIfNotNull<T>(Action<T?> setter, T? value) where T : struct
      => SetIfNotNull(setter, value);
    
    public void CallUpdate<T>(T? value, Action<T> setter) where T : struct
      => Update(value, setter);
    
    public void CallUpdate<T>(T? value, Action<T?> setter) where T : struct
      => Update(value, setter);
  }
}

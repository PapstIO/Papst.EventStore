#nullable enable
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Papst.EventStore.Aggregation;
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

    _sut.CallSetIfNotNull<int>(v => result = v, value);

    result.Should().Be(42);
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetter_WhenNullableStructIsNull()
  {
    int result = 0;
    int? value = null;

    _sut.CallSetIfNotNull<int>(v => result = v, value);

    result.Should().Be(0);
  }

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetter_WhenNullableDateTimeHasValue()
  {
    DateTime result = default;
    DateTime? value = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    _sut.CallSetIfNotNull<DateTime>(v => result = v, value);

    result.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetter_WhenNullableDateTimeIsNull()
  {
    DateTime result = default;
    DateTime? value = null;

    _sut.CallSetIfNotNull<DateTime>(v => result = v, value);

    result.Should().Be(default(DateTime));
  }

  [Fact]
  public void SetIfNotNull_ShouldInvokeSetter_WhenNullableGuidHasValue()
  {
    Guid result = Guid.Empty;
    Guid expected = Guid.NewGuid();
    Guid? value = expected;

    _sut.CallSetIfNotNull<Guid>(v => result = v, value);

    result.Should().Be(expected);
  }

  [Fact]
  public void SetIfNotNull_ShouldNotInvokeSetter_WhenNullableGuidIsNull()
  {
    Guid result = Guid.Empty;
    Guid? value = null;

    _sut.CallSetIfNotNull<Guid>(v => result = v, value);

    result.Should().Be(Guid.Empty);
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
  }
}

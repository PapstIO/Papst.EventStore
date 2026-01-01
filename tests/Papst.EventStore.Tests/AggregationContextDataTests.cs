using System;
using AutoFixture.Xunit2;
using FluentAssertions;
using Xunit;

namespace Papst.EventStore.Tests;

public class AggregationContextDataTests
{
  private static IAggregatorStreamContext CreateContext(ulong currentVersion)
  {
    return new Papst.EventStore.Aggregation.EventRegistration.EventRegistrationEventAggregatorStreamContext(
      StreamId: Guid.NewGuid(),
      TargetVersion: 0UL,
      CurrentVersion: currentVersion,
      StreamCreated: DateTimeOffset.UtcNow,
      EventTime: DateTimeOffset.UtcNow);
  }

  [Theory, AutoData]
  public void TestAggregationContextDataInitialization(string key, ulong version, ulong? validUntil, string value)
  {
    var data = new AggregationContextData(key, version, validUntil, value);

    data.Should().NotBeNull();
    data.Key.Should().Be(key);
    data.Version.Should().Be(version);
    data.ValidUntilVersion.Should().Be(validUntil);
    data.Value.Should().Be(value);
  }

  [Fact]
  public void TestAggregationContextDataEquality()
  {
    var d1 = new AggregationContextData("key", 1UL, 5UL, "value");
    var d2 = new AggregationContextData("key", 1UL, 5UL, "value");

    d1.Should().Be(d2);
  }

  [Fact]
  public void TestAggregationContextDataInequalityDifferentKey()
  {
    var d1 = new AggregationContextData("k1", 1UL, 5UL, "value");
    var d2 = new AggregationContextData("k2", 1UL, 5UL, "value");

    d1.Should().NotBe(d2);
  }

  [Fact]
  public void TestAggregationContextDataInequalityDifferentVersion()
  {
    var d1 = new AggregationContextData("key", 1UL, 5UL, "value");
    var d2 = new AggregationContextData("key", 2UL, 5UL, "value");

    d1.Should().NotBe(d2);
  }

  [Fact]
  public void SetAndGetAggregationData_WhenValid_ReturnsData()
  {
    var ctx = CreateContext(currentVersion: 5UL);

    ctx.SetAggregationData("key1", 4UL, "value1");

    var data = ctx.GetAggregationData("key1");

    data.Should().NotBeNull();
    data!.Key.Should().Be("key1");
    data.Value.Should().Be("value1");
    data.Version.Should().Be(4UL);
    data.ValidUntilVersion.Should().BeNull();
  }

  [Fact]
  public void GetAggregationData_WithVersionGreaterThanCurrent_ReturnsNull()
  {
    var ctx = CreateContext(currentVersion: 3UL);

    ctx.SetAggregationData("k", 4UL, "v");

    var data = ctx.GetAggregationData("k");

    data.Should().BeNull();
  }

  [Fact]
  public void GetAggregationData_ExpiredByValidUntil_ReturnsNull()
  {
    var ctx = CreateContext(currentVersion: 6UL);

    ctx.SetAggregationData("k2", 2UL, "v2", validUntilVersion: 5UL);

    var data = ctx.GetAggregationData("k2");

    data.Should().BeNull();
  }

  [Fact]
  public void GetAggregationData_IgnoreValidity_ReturnsDataEvenIfExpiredOrNew()
  {
    var ctx = CreateContext(currentVersion: 6UL);

    // data with version higher than current
    ctx.SetAggregationData("newer", 10UL, "nv");
    // data expired
    ctx.SetAggregationData("expired", 2UL, "ev", validUntilVersion: 5UL);

    var newer = ctx.GetAggregationData("newer", ignoreValidity: true);
    var expired = ctx.GetAggregationData("expired", ignoreValidity: true);

    newer.Should().NotBeNull();
    expired.Should().NotBeNull();
  }
}

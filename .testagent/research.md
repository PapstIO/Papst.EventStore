# Test Generation Research: ILowLevelEventStore

## Project Overview

- **Path**: `/Users/marco/projects/copilot-worktrees/Papst.EventStore/mpapst-musical-adventure`
- **Repository**: PapstIO/Papst.EventStore
- **Language**: C#
- **Framework**: .NET 10.0
- **Test Framework**: xUnit with AutoFixture.Xunit2, Shouldly assertions, Moq mocking
- **Solution File**: `Papst.EventStore.slnx` (Visual Studio 2022+)

## Coverage Baseline

- **Initial Line Coverage**: Unknown (no pre-computed coverage data found in `.testagent/`)
- **Initial Branch Coverage**: Unknown
- **Strategy**: Broad (comprehensive new tests for ILowLevelEventStore API)
- **Existing Test Count**: 
  - 3 implementation-specific test projects with reference tests
  - Only 1 implementation (AzureCosmos) has visible ILowLevelEventStream tests
  - InMemory and MongoDB test projects lack explicit ILowLevelEventStream coverage
  - EntityFrameworkCore and FileSystem implementations lack visible test projects

## Build & Test Commands

- **Build**: `dotnet build --configuration Debug Papst.EventStore.slnx`
- **Test**: `dotnet test --no-build --verbosity normal --configuration Debug Papst.EventStore.slnx`
- **Test Single Project**: `dotnet test --no-build --verbosity normal --configuration Debug tests/[ProjectName]`
- **Test Filter**: `dotnet test --filter "ClassName=TestClass"`

### CI/CD Reference
Located in `.github/workflows/test.yml` — uses above commands with Debug configuration.

## Project Structure

### Source Directory: `/src/`

**Core Interfaces:**
- `Papst.EventStore/ILowLevelEventStore.cs` — Low-level store interface (GetAsync, CreateAsync)
- `Papst.EventStore/ILowLevelEventStream.cs` — Stream append interface
- `Papst.EventStore/IEventStore.cs` — Generic high-level store interface (referenced)
- `Papst.EventStore/IEventStream.cs` — Generic stream interface (referenced)

**Core Models:**
- `Papst.EventStore/Documents/EventStreamDocument.cs` — Event document structure
- `Papst.EventStore/Documents/EventStreamMetaData.cs` — Metadata container (UserId, UserName, TenantId, Comment, Additional dict)

**Exception Types:**
- `Papst.EventStore/Exceptions/EventStreamAlreadyExistsException.cs`
- `Papst.EventStore/Exceptions/EventStreamNotFoundException.cs`

**Implementations:**
- `Papst.EventStore.InMemory/InMemoryLowLevelEventStore.cs` — Dictionary-based in-memory storage
- `Papst.EventStore.InMemory/InMemoryEventStream.cs` — Implements both IEventStream and ILowLevelEventStream
- `Papst.EventStore.MongoDB/MongoDBLowLevelEventStore.cs` — MongoDB implementation
- `Papst.EventStore.EntityFrameworkCore/EFCoreLowLevelEventStore.cs` — EF Core implementation
- `Papst.EventStore.FileSystem/FileSystemLowLevelEventStore.cs` — File-based implementation

### Test Directory: `/tests/`

**Test Projects:**
1. `Papst.EventsStore.InMemory.Tests/` — InMemory implementation tests
   - Fixture: `InMemoryTestFixture` (simple ServiceCollection setup, no async)
   - Test file: `IntegrationTests/InMemoryEventStoreTests.cs` (IEventStore tests)
   - Status: ILowLevelEventStream tests not yet present

2. `Papst.EventStore.MongoDB.Tests/` — MongoDB implementation tests
   - Fixture: `MongoDBIntegrationTestFixture` (Testcontainers.MongoDb, IAsyncLifetime)
   - Test file: `IntegrationTests/MongoDBEventStoreTests.cs` (IEventStore tests)
   - Status: ILowLevelEventStream tests not yet present

3. `Papst.EventStore.AzureCosmos.Tests/` — Azure Cosmos DB implementation tests
   - Fixture: `CosmosDbIntegrationTestFixture` (Testcontainers.CosmosDb, custom WaitUntil)
   - Test file: `IntegrationTests/CosmosEventStreamTests.cs` (only implementation with ILowLevelEventStream tests)
   - Status: ILowLevelEventStream tests present (lines 70-91)

4. `Papst.EventStore.Tests/` — Core library unit tests
   - Framework: Moq, CodeGeneration
   - Tests for core models and exception types

5. `Papst.EventStore.CodeGeneration.Tests/` — Code generation unit tests
   - Tests for code generation logic

## ILowLevelEventStore Interface Definition

**Location**: `src/Papst.EventStore/ILowLevelEventStore.cs` (lines 7-55)

### Method: GetAsync (read)
```csharp
Task<ILowLevelEventStream> GetAsync(Guid streamId, CancellationToken cancellationToken = default);
```

**Purpose**: Retrieve an existing event stream by ID.

**Throws**: 
- `EventStreamNotFoundException` — if stream with given ID does not exist

**Notes**:
- Returns `ILowLevelEventStream` for append operations
- Guid is the unique stream identifier
- Cancellation token allows async cancellation

### Methods: CreateAsync (write)

**Simple Overload** (delegates to full overload):
```csharp
Task<ILowLevelEventStream> CreateAsync(
    Guid streamId, 
    string targetTypeName, 
    CancellationToken cancellationToken = default);
```

**Full Overload** (with comprehensive metadata):
```csharp
Task<ILowLevelEventStream> CreateAsync(
    Guid streamId,
    string targetTypeName,
    string? tenantId,
    string? userId,
    string? userName,
    string? comment,
    Dictionary<string, string>? additionalMetaData,
    CancellationToken cancellationToken = default);
```

**Purpose**: Create a new event stream with optional metadata.

**Parameters**:
- `streamId` — Unique Guid identifier for the stream
- `targetTypeName` — Type name for events (unknown at compile time, resolved at runtime)
- `tenantId` — Optional tenant identifier (null-safe)
- `userId` — Optional user ID who created the stream (null-safe)
- `userName` — Optional user name (null-safe)
- `comment` — Optional descriptive comment (null-safe)
- `additionalMetaData` — Optional custom key-value pairs (null-safe dictionary)
- `cancellationToken` — Async cancellation token

**Returns**: `ILowLevelEventStream` for immediate append operations

**Throws**: 
- `EventStreamAlreadyExistsException` — if stream with given ID already exists

**Notes**:
- Simple overload calls full overload with null metadata
- All metadata parameters are nullable strings
- All metadata parameters are optional for API flexibility
- Stream creation succeeds if no duplicate exists

## ILowLevelEventStream Interface Definition

**Location**: `src/Papst.EventStore/ILowLevelEventStream.cs` (lines 6-25)

### Method: AppendAsync (low-level event append)
```csharp
Task AppendAsync(
    Guid id,
    string eventType,
    JObject evt,
    EventStreamMetaData? metaData = null,
    CancellationToken cancellationToken = default);
```

**Purpose**: Append a raw event (as JObject) to the stream with runtime-determined type.

**Parameters**:
- `id` — The stream ID (Guid)
- `eventType` — String name of event type (not compile-time known, can be any string)
- `evt` — Raw event payload as `Newtonsoft.Json.Linq.JObject` (JSON structure)
- `metaData` — Optional event-level metadata (nullable EventStreamMetaData)
- `cancellationToken` — Async cancellation token

**Returns**: Task (fire-and-forget append, no event ID returned)

**Notes**:
- Uses `Newtonsoft.Json.Linq.JObject` for flexible JSON payloads
- `eventType` is a string, allowing arbitrary event type names
- This method enables storing events with unknown types at compile time
- Metadata is optional per-event (separate from stream-level metadata)
- Append-only semantics (events cannot be modified or deleted)

## EventStreamMetaData Structure

**Location**: `src/Papst.EventStore/Documents/EventStreamMetaData.cs`

**Definition**:
```csharp
public record EventStreamMetaData
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? TenantId { get; set; }
    public string? Comment { get; set; }
    public Dictionary<string, string>? Additional { get; set; }
}
```

**Properties**:
- All properties are nullable strings (except `Additional` which is nullable Dictionary)
- `Additional` dict for custom key-value pairs (null-safe)
- Used at stream-creation time and optionally per-event append

**Test Patterns Observed**:
- Metadata can be populated completely: `new() { UserId = "user1", TenantId = "tenant1", ... }`
- Metadata can be partially populated: `new() { Comment = "test event" }`
- Metadata can be null entirely: `null` passed to AppendAsync
- Metadata round-trips through storage unchanged

## Exception Types

### EventStreamAlreadyExistsException

**Location**: `src/Papst.EventStore/Exceptions/EventStreamAlreadyExistsException.cs`

**Inheritance**: Extends `EventStreamException`

**Thrown by**: `CreateAsync()` methods when attempting to create duplicate stream

**Constructor Overloads**:
```csharp
public EventStreamAlreadyExistsException(Guid streamId, string message)
public EventStreamAlreadyExistsException(string message)
public EventStreamAlreadyExistsException(string message, Exception innerException)
```

**Test Pattern** (from MongoDBEventStoreTests.cs lines 54-66):
```csharp
// Create stream
var streamId = Guid.NewGuid();
await store.CreateAsync(streamId, "MyEvent");

// Attempt duplicate creation
var ex = await Assert.ThrowsAsync<EventStreamAlreadyExistsException>(
    () => store.CreateAsync(streamId, "MyEvent"));
```

### EventStreamNotFoundException

**Location**: `src/Papst.EventStore/Exceptions/EventStreamNotFoundException.cs`

**Inheritance**: Extends `EventStreamException`

**Thrown by**: `GetAsync()` method when stream ID does not exist

**Constructor Overloads**:
```csharp
public EventStreamNotFoundException(Guid streamId, string message)
public EventStreamNotFoundException(string message)
public EventStreamNotFoundException(string message, Exception innerException)
```

**Test Pattern** (from MongoDBEventStoreTests.cs lines 68-79):
```csharp
// Attempt to get non-existent stream
var nonExistentId = Guid.NewGuid();
var ex = await Assert.ThrowsAsync<EventStreamNotFoundException>(
    () => store.GetAsync(nonExistentId));
```

## JObject Event Patterns

**Import**: `using Newtonsoft.Json.Linq;`

### Creating Events from Anonymous Objects

**Pattern from CosmosEventStreamTests.cs (line 78)**:
```csharp
var evt = new JObject { ["Name"] = "TestEvent", ["Version"] = 1 };
```

**Alternative Construction**:
```csharp
var evt = JObject.FromObject(new { Name = "TestEvent", Version = 1 });
```

### Appending JObject Events

**Pattern from CosmosEventStreamTests.cs (lines 78-81)**:
```csharp
var stream = await store.CreateAsync(streamId, "TestEvent");
await stream.AppendAsync(
    streamId,
    "TestEventType",
    new JObject { ["Name"] = "TestEvent", ["Version"] = 1 },
    new EventStreamMetaData { UserId = "user1" }
);
```

### Verifying JObject Contents

**Assertion Pattern** (Shouldly):
```csharp
evt["Name"].ToString().ShouldBe("TestEvent");
evt["Version"].Value<int>().ShouldBe(1);
evt.ShouldContainKey("Name");
```

### Null Value Handling

**Creating with null values**:
```csharp
var evt = new JObject { ["Name"] = null, ["Version"] = 1 };
```

**Conditional property access**:
```csharp
evt["Name"]?.ToString().ShouldBeNull();
```

## Test Fixture Patterns

### InMemory Fixture (Non-Async)

**Location**: `tests/Papst.EventsStore.InMemory.Tests/InMemoryTestFixture.cs`

**Pattern**: Simple synchronous setup, no container lifecycle needed.

```csharp
public class InMemoryTestFixture
{
    private ServiceProvider? _serviceProvider;
    
    private ServiceProvider ServiceProvider =>
        _serviceProvider ??= new ServiceCollection()
            .AddInMemoryEventStore()
            .BuildServiceProvider();
    
    public ILowLevelEventStore GetStore() =>
        ServiceProvider.GetRequiredService<ILowLevelEventStore>();
}
```

**Usage in Test Class**:
```csharp
public class InMemoryEventStoreTests : IClassFixture<InMemoryTestFixture>
{
    private readonly InMemoryTestFixture _fixture;
    
    public InMemoryEventStoreTests(InMemoryTestFixture fixture) =>
        _fixture = fixture;
    
    [Theory, AutoData]
    public async Task CreateAsync_WithValidStreamId_CreatesStream(Guid streamId)
    {
        var store = _fixture.GetStore();
        var stream = await store.CreateAsync(streamId, "TestEvent");
        stream.ShouldNotBeNull();
    }
}
```

**Key Points**:
- Lazy initialization: `_serviceProvider ??= ...`
- No async setup needed
- Fixture injected via xUnit's `IClassFixture<T>` interface
- Simple `ServiceCollection` registration pattern
- Extension method `.AddInMemoryEventStore()` for DI setup

### MongoDB Fixture (Async with Testcontainers)

**Location**: `tests/Papst.EventStore.MongoDB.Tests/MongoDBIntegrationTestFixture.cs`

**Pattern**: Testcontainers-based async lifecycle management.

```csharp
public class MongoDBIntegrationTestFixture : IAsyncLifetime
{
    private MongoDbContainer? _container;
    private ServiceProvider? _serviceProvider;
    
    public async Task InitializeAsync()
    {
        _container = new MongoDbBuilder()
            .WithImage("mongo:7.0")
            .Build();
        
        await _container.StartAsync();
        
        _serviceProvider = new ServiceCollection()
            .AddMongoDBEventStore(_container.GetConnectionString())
            .BuildServiceProvider();
    }
    
    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
            await _serviceProvider.DisposeAsync();
        
        if (_container is not null)
            await _container.StopAsync();
    }
    
    public ILowLevelEventStore GetStore() =>
        _serviceProvider!.GetRequiredService<ILowLevelEventStore>();
}
```

**Usage in Test Class**:
```csharp
public class MongoDBEventStoreTests : IAsyncLifetime
{
    private readonly MongoDBIntegrationTestFixture _fixture = new();
    
    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();
    
    [Theory, AutoData]
    public async Task CreateAsync_WithValidStreamId_CreatesStream(Guid streamId)
    {
        var store = _fixture.GetStore();
        var stream = await store.CreateAsync(streamId, "TestEvent");
        stream.ShouldNotBeNull();
    }
}
```

**Key Points**:
- Implements `IAsyncLifetime` interface (xUnit pattern for async fixtures)
- Container started in `InitializeAsync()`
- Container stopped in `DisposeAsync()`
- Connection string obtained from running container
- Extension method `.AddMongoDBEventStore(connectionString)` for DI setup
- Image specified: `mongo:7.0`

### CosmosDb Fixture (Advanced Async with Custom WaitUntil)

**Location**: `tests/Papst.EventStore.AzureCosmos.Tests/CosmosDbIntegrationTestFixture.cs`

**Pattern**: Advanced Testcontainers with custom health check strategy.

```csharp
public class CosmosDbIntegrationTestFixture : IAsyncLifetime
{
    private CosmosDbContainer? _container;
    private ServiceProvider? _serviceProvider;
    
    public async Task InitializeAsync()
    {
        var strategy = new CosmosDbWaitUntilStrategy()
            .UntilResourceCreated("cosmosdb", "TestDb");
        
        _container = new CosmosDbBuilder()
            .WithImage("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator")
            .WithEnvironment("COSMOS_DB_ENDPOINT", "...")
            .WithWaitStrategy(strategy)
            .Build();
        
        await _container.StartAsync();
        
        _serviceProvider = new ServiceCollection()
            .AddCosmosEventStore(connectionString)
            .BuildServiceProvider();
    }
    
    public async Task DisposeAsync()
    {
        if (_serviceProvider is not null)
            await _serviceProvider.DisposeAsync();
        
        if (_container is not null)
            await _container.StopAsync();
    }
    
    public ILowLevelEventStream GetStream() =>
        _serviceProvider!.GetRequiredService<ILowLevelEventStream>();
}
```

**Key Points**:
- Implements `IAsyncLifetime` for async lifecycle
- Custom `WaitUntilStrategy` for CosmosDb emulator health checks
- Container image: Cosmos DB Linux emulator (mcr.microsoft.com/...)
- Environment variable configuration for Cosmos DB setup
- Extension method `.AddCosmosEventStore(connectionString)`

## Test Framework & Patterns

### Testing Framework Stack

- **Test Framework**: xUnit (1.x or 2.x — modern version)
- **Assertion Library**: Shouldly (fluent assertions)
- **Mocking**: Moq
- **Parameterization**: AutoFixture.Xunit2 `[Theory, AutoData]`
- **Dependency Injection**: xUnit's `IClassFixture<T>` and `IAsyncLifetime`

### xUnit Test Pattern

**Basic Test Method**:
```csharp
[Theory, AutoData]
public async Task MethodName_Scenario_ExpectedOutcome(Guid streamId)
{
    // Arrange
    var store = _fixture.GetStore();
    
    // Act
    var stream = await store.CreateAsync(streamId, "TestEvent");
    
    // Assert
    stream.ShouldNotBeNull();
}
```

**Key Conventions**:
- `[Theory]` for parameterized tests (vs `[Fact]` for fixed tests)
- `[AutoData]` automatically generates test parameters (Guid, string, etc.)
- `async Task` for async operations
- CancellationToken typically passed via AutoFixture

### Shouldly Assertion Patterns

| Pattern | Example |
|---------|---------|
| Null checks | `.ShouldBeNull()` / `.ShouldNotBeNull()` |
| Equality | `.ShouldBe(expected)` |
| Collection contains | `.ShouldContain(item)` |
| Collection empty | `.ShouldBeEmpty()` |
| Type checks | `.ShouldBeAssignableTo<T>()` |
| String contains | `.ShouldContain(substring)` |

### Exception Testing Pattern

**xUnit Exception Assertions**:
```csharp
[Theory, AutoData]
public async Task CreateAsync_WithDuplicateStreamId_ThrowsException(Guid streamId)
{
    var store = _fixture.GetStore();
    
    // Create first stream
    await store.CreateAsync(streamId, "TestEvent");
    
    // Attempt duplicate
    var ex = await Assert.ThrowsAsync<EventStreamAlreadyExistsException>(
        () => store.CreateAsync(streamId, "TestEvent"));
    
    ex.ShouldNotBeNull();
}
```

## Existing Test Coverage Analysis

### Current ILowLevelEventStream Tests

**Only Implementation with Visible Tests**: AzureCosmos

**Location**: `tests/Papst.EventStore.AzureCosmos.Tests/IntegrationTests/CosmosEventStreamTests.cs` (lines 70-91)

**Test: LowLevelAppendAsync**
```csharp
[Theory, AutoData]
public async Task LowLevelAppendAsync_WithValidEvent_AppendsEvent(Guid streamId)
{
    var stream = await _store.CreateAsync(streamId, "TestEvent");
    var evt = new JObject { ["Name"] = "TestEvent", ["Version"] = 1 };
    
    await stream.AppendAsync(streamId, "TestEventType", evt);
    
    // Verify event was appended (implementation-specific retrieval)
    var retrieved = await stream.GetAsync(streamId);
    retrieved.ShouldNotBeNull();
}
```

### Gap Analysis

| Implementation | IEventStore Tests | ILowLevelEventStream Tests | Status |
|---|---|---|---|
| InMemory | ✓ Present | ✗ Missing | Needs coverage |
| MongoDB | ✓ Present | ✗ Missing | Needs coverage |
| AzureCosmos | ✓ Present | ✓ Present (1 test) | Minimal coverage |
| EntityFrameworkCore | Unknown | Unknown | No visible test project |
| FileSystem | Unknown | Unknown | No visible test project |

### IEventStore Test Patterns (Reference for Low-Level Tests)

**From MongoDBEventStoreTests.cs**:

1. **CreateAsync with metadata** (lines 34-52)
2. **Duplicate creation exception** (lines 54-66)
3. **Non-existent stream exception** (lines 68-79)
4. **Stream version tracking** (implied from MongoDb tests)
5. **Metadata preservation** (tested through GetAsync retrieval)

## Test Generation Scope

### Files to Test

#### High Priority (Core Low-Level API)

| File | Methods | Testability | Notes |
|------|---------|-------------|-------|
| `ILowLevelEventStore.cs` | GetAsync, CreateAsync (2 overloads) | High | All implementations must pass same tests |
| `ILowLevelEventStream.cs` | AppendAsync | High | All implementations must pass same tests |
| EventStreamMetaData | Properties | High | Serialization/deserialization |
| EventStreamAlreadyExistsException | All constructors | High | Exception creation and properties |
| EventStreamNotFoundException | All constructors | High | Exception creation and properties |

#### Medium Priority (Implementation-Specific)

| Implementation | Test Files | Testability | Notes |
|---|---|---|---|
| InMemory | `InMemoryLowLevelEventStore.cs` | High | In-memory dict backend, no I/O |
| MongoDB | `MongoDBLowLevelEventStore.cs` | Medium | Requires Testcontainers, async |
| AzureCosmos | `CosmosLowLevelEventStore.cs` | Medium | Requires Testcontainers, custom config |
| EntityFrameworkCore | `EFCoreLowLevelEventStore.cs` | Medium | Multiple backend options |
| FileSystem | `FileSystemLowLevelEventStore.cs` | High | File system I/O, deterministic |

#### Low Priority / Skip

| File | Reason |
|------|--------|
| `IEventStore.cs` (high-level API) | Separate scope, high-level tests exist |
| `IEventStream.cs` (high-level API) | Separate scope, high-level tests exist |
| Code generation utilities | Infrastructure, tested separately |
| Auto-generated models | Framework-generated, implementation-specific |

## Testing Recommendations

### Priority 1: Interface Contract Tests

Create a **shared test suite** for ILowLevelEventStore and ILowLevelEventStream:
- All 4 implementations (InMemory, MongoDB, AzureCosmos, EntityFrameworkCore) share exact same interface contract
- Write contract tests once, parameterize by fixture type
- Each implementation provides its own fixture (InMemory simple, others Testcontainers-based)

**Test Areas**:
1. **CreateAsync API Coverage**
   - Simple overload (stream ID + type only)
   - Full overload (all metadata parameters)
   - Metadata null-handling
   - Exception on duplicate creation
   - Successful stream creation returns valid ILowLevelEventStream

2. **GetAsync API Coverage**
   - Retrieve existing stream returns valid instance
   - Non-existent stream throws EventStreamNotFoundException
   - Retrieved stream is ready for AppendAsync

3. **AppendAsync API Coverage**
   - Append with minimal parameters (id, type, JObject)
   - Append with event-level metadata
   - Append multiple events to same stream
   - Metadata round-trips unchanged
   - Event type strings preserved exactly
   - JObject payloads preserved unchanged

4. **Exception Contract**
   - EventStreamAlreadyExistsException constructors
   - EventStreamNotFoundException constructors
   - Exception properties and messages
   - Exception inheritance hierarchy

5. **Metadata Contract**
   - All properties nullable
   - Dictionary Additional preserves keys/values
   - Null metadata handled gracefully
   - Partial metadata accepted

### Priority 2: Implementation-Specific Tests

For each implementation after contract tests:
- Persistence verification (data survives lifecycle)
- Concurrent append handling
- Edge cases (very large payloads, special characters in type names)
- Performance characteristics (if relevant)

### Priority 3: Integration Scenarios

- Multi-implementation verification (same operations across InMemory, MongoDB, AzureCosmos)
- Fixture lifecycle (proper cleanup, no state leakage between tests)
- Cancellation token handling

## Build and Test Execution

### Prerequisites

- .NET 10 SDK installed
- Docker installed (for MongoDB/CosmosDb Testcontainers)
- Visual Studio 2022+ or VS Code with C# support

### Build Solution

```bash
# Full build
dotnet build --configuration Debug Papst.EventStore.slnx

# Build specific project
dotnet build --configuration Debug tests/Papst.EventStore.AzureCosmos.Tests
```

### Run Tests

```bash
# All tests
dotnet test --no-build --verbosity normal --configuration Debug Papst.EventStore.slnx

# Specific test project
dotnet test --no-build --verbosity normal --configuration Debug tests/Papst.EventsStore.InMemory.Tests

# Specific test class
dotnet test --no-build --filter "ClassName=CosmosEventStreamTests" --configuration Debug

# Specific test method
dotnet test --no-build --filter "Name~LowLevelAppendAsync" --configuration Debug
```

### Debugging Tests

```bash
# Run with verbose output
dotnet test --verbosity detailed --configuration Debug

# Run single test with output
dotnet test --filter "Name~SpecificTestName" --verbosity detailed --configuration Debug
```

## Known Gaps and Considerations

1. **Missing ILowLevelEventStream Tests**: 
   - InMemory implementation has no visible low-level tests
   - MongoDB implementation has no visible low-level tests
   - Only AzureCosmos has 1 reference test

2. **Missing Test Projects**:
   - EntityFrameworkCore and FileSystem implementations lack dedicated integration test projects
   - Unclear if these are tested elsewhere or untested

3. **API Documentation Note**:
   - README states "This API is currently only implemented by the Azure Cosmos provider" (contradicts visible code)
   - All 4 implementations clearly exist and have low-level store implementations
   - Tests should verify all implementations

4. **Metadata Strategy**:
   - Stream-level metadata (CreateAsync) vs event-level metadata (AppendAsync)
   - Unclear if both should always be populated or if they're independent
   - Test should verify both paths work independently and together

## Test File Organization Recommendation

**Suggested new test files**:

```
tests/Papst.EventsStore.InMemory.Tests/
  IntegrationTests/
    InMemoryLowLevelEventStoreTests.cs  (NEW)
    InMemoryLowLevelEventStreamTests.cs (NEW)

tests/Papst.EventStore.MongoDB.Tests/
  IntegrationTests/
    MongoDBLowLevelEventStoreTests.cs   (NEW)
    MongoDBLowLevelEventStreamTests.cs  (NEW)

tests/Papst.EventStore.AzureCosmos.Tests/
  IntegrationTests/
    CosmosLowLevelEventStreamTests.cs   (EXPAND existing 70-91)

Shared (optional):
  LowLevelEventStoreContractTests.cs    (Parameterized by fixture)
  LowLevelEventStreamContractTests.cs   (Parameterized by fixture)
```

Each test class:
- Uses xUnit `[Theory, AutoData]` pattern
- Inherits from fixture (e.g., `IClassFixture<InMemoryTestFixture>`)
- Uses Shouldly assertions
- Follows AAA (Arrange-Act-Assert) structure
- Tests both happy paths and exception cases

## References

- xUnit Documentation: https://xunit.net/docs/getting-started/netcore
- Shouldly Assertions: https://shouldly.readthedocs.io/
- AutoFixture: https://github.com/AutoFixture/AutoFixture
- Testcontainers.DotNet: https://testcontainers.com/docs/dotnet/
- Newtonsoft.Json.Linq: https://www.newtonsoft.com/json/help/html/N_Newtonsoft_Json_Linq.htm

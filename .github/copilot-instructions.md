# Copilot Instructions for Papst.EventStore

## Project Overview

Papst.EventStore is a .NET Event Sourcing library that provides an abstraction layer for storing and replaying events in a database. It ships with multiple backend implementations (Azure Cosmos DB, Entity Framework Core, MongoDB, FileSystem, InMemory) and a code-generation package that auto-registers events and aggregators via source generators.

## Repository Layout

```
src/
  Papst.EventStore/                          # Core abstractions (interfaces, base classes)
  Papst.EventStore.Aggregation.EventRegistration/  # Code-generated event registration helpers
  Papst.EventStore.AzureCosmos/             # Azure Cosmos DB backend
  Papst.EventStore.CodeGeneration/          # Roslyn source generator
  Papst.EventStore.EntityFrameworkCore/     # EF Core backend
  Papst.EventStore.FileSystem/              # FileSystem backend (testing only)
  Papst.EventStore.InMemory/                # In-memory backend (testing only)
  Papst.EventStore.MongoDB/                 # MongoDB backend
tests/
  Papst.EventStore.Tests/                   # Unit tests for core library
  Papst.EventStore.AzureCosmos.Tests/
  Papst.EventStore.CodeGeneration.Tests/
  Papst.EventStore.MongoDB.Tests/
  Papst.EventsStore.InMemory.Tests/
samples/                                     # Sample applications
```

## Technology Stack

- **Language**: C# on .NET 10
- **Test framework**: xUnit with AutoFixture, FluentAssertions, and Moq
- **Serialization**: Newtonsoft.Json
- **Versioning**: Nerdbank.GitVersioning (`version.json`)
- **Package management**: Central package management via `Directory.Packages.props`

## Build, Test & Lint

```bash
# Build everything
dotnet build Papst.EventStore.slnx

# Run all tests
dotnet test --configuration Debug Papst.EventStore.slnx

# Build or test a specific project
dotnet build src/Papst.EventStore/Papst.EventStore.csproj
dotnet test tests/Papst.EventStore.Tests/Papst.EventStore.Tests.csproj
```

The CI pipeline (`.github/workflows/test.yml`) runs build + test on every pull request targeting `main`.

## Coding Conventions

- **Nullable reference types** are enabled in all projects (`<Nullable>enable</Nullable>`).
- **Implicit usings** are disabled; every `using` must be declared explicitly (see `Usings.cs` in each project for common usings).
- Interfaces live in `src/Papst.EventStore/` and are prefixed with `I` (e.g., `IEventStore`, `IEventStream`).
- Implementations go in the appropriate backend project and are registered through extension methods on `IServiceCollection`.
- Exceptions are placed in an `Exceptions/` subfolder and inherit from a common base where one exists.
- Internal types that need to be visible to the test project are exposed via `[InternalsVisibleTo("...Tests")]` in the `.csproj`.

## Key Abstractions

| Type | Purpose |
|------|---------|
| `IEventStore` | Create or retrieve an `IEventStream` |
| `IEventStream` | Append events, read events (with paging), update metadata |
| `IEventStreamAggregator` | Apply events to an entity aggregate using `IEventAggregator<TEntity, TEvent>` handlers |
| `IEventAggregator<TEntity, TEvent>` | Single-event handler; implement or extend `EventAggregatorBase<TEntity, TEvent>` |
| `IEventTypeProvider` | Maps event name strings to CLR types |
| `[EventName]` attribute | Decorates event classes; used by the source generator to auto-register types |

## Event Registration via Code Generation

Decorate event classes with `[EventName("MyEventV2")]`. The source generator in `Papst.EventStore.CodeGeneration` picks these up at compile time and emits an `AddCodeGeneratedEvents()` extension method for `IServiceCollection`.

Multiple `[EventName]` attributes are supported (for versioning/migration). Set `IsWriteName = false` on legacy names to keep them as read aliases only.

## Adding a New Backend Implementation

1. Create a new project under `src/` targeting `net10.0`.
2. Reference `Papst.EventStore` (core abstractions).
3. Implement `IEventStore` and `IEventStream`.
4. Provide an `AddXxx(this IServiceCollection)` extension method for DI registration.
5. Add a matching test project under `tests/`.
6. Add both projects to `Papst.EventStore.slnx` (and the appropriate sub-solution if one exists).

## Pull Request Guidelines

- All changes must build and pass tests (`dotnet build` + `dotnet test` on `Papst.EventStore.slnx`).
- Keep PRs focused; one concern per PR.
- Follow the existing XML doc-comment style on public APIs.
- Do not introduce new NuGet dependencies without updating `Directory.Packages.props`.

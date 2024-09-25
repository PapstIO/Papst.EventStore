# Entity Framework Core Implementation of Event Stream

The Entity Framework Implementation needs to be added to the dependency injection container.

To achieve this, there is an extension method to the `IServiceCollection` available, that takes a Configuration action for the `DbContextOptionsBuilder`:
```csharp
IServiceCollection services;

services.AddEntityFrameworkCoreEventStore(options => options.AddSqlServer("..."));
```

## Configuration

No further configuration is necessary.

## Migrations / Table creation

Tables are created using Entity Framework Core Migrations.
These Migrations are not applied automatically. This needs to be done by additional code or by applying the .sql files in the `Migrations` directory of the Source Code Repository.

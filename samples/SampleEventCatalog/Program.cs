using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.EventCatalog;

namespace SampleEventCatalog;

public class Program
{
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();

        // Register both generated events and catalog
        services.AddCodeGeneratedEvents();
        services.AddCodeGeneratedEventCatalog();

        var serviceProvider = services.BuildServiceProvider();
        var catalog = serviceProvider.GetRequiredService<IEventCatalog>();

        Console.WriteLine("=== Event Catalog Sample ===");
        Console.WriteLine();

        // List all events for User entity
        Console.WriteLine("--- Events for User entity ---");
        var userEvents = catalog.ListEvents<User>();
        foreach (var entry in userEvents)
        {
            Console.WriteLine($"  Event: {entry.EventName}");
            Console.WriteLine($"    Description: {entry.Description ?? "(none)"}");
            Console.WriteLine($"    Constraints: {(entry.Constraints != null ? string.Join(", ", entry.Constraints) : "(none)")}");
            Console.WriteLine();
        }

        // List all events for Order entity
        Console.WriteLine("--- Events for Order entity ---");
        var orderEvents = catalog.ListEvents<Order>();
        foreach (var entry in orderEvents)
        {
            Console.WriteLine($"  Event: {entry.EventName}");
            Console.WriteLine($"    Description: {entry.Description ?? "(none)"}");
            Console.WriteLine($"    Constraints: {(entry.Constraints != null ? string.Join(", ", entry.Constraints) : "(none)")}");
            Console.WriteLine();
        }

        // Filter events by constraint
        Console.WriteLine("--- User events with 'Update' constraint ---");
        var updateEvents = catalog.ListEvents<User>(constraints: new[] { "Update" });
        foreach (var entry in updateEvents)
        {
            Console.WriteLine($"  Event: {entry.EventName} ({entry.Description})");
        }
        Console.WriteLine();

        // Filter events by name
        Console.WriteLine("--- Looking up 'UserCreated' event ---");
        var filtered = catalog.ListEvents<User>(name: "UserCreated");
        foreach (var entry in filtered)
        {
            Console.WriteLine($"  Found: {entry.EventName} - {entry.Description}");
        }
        Console.WriteLine();

        // Get event details with JSON schema
        Console.WriteLine("--- Event Details with JSON Schema ---");
        var details = catalog.GetEventDetails("OrderPlaced");
        if (details != null)
        {
            Console.WriteLine($"  Event: {details.EventName}");
            Console.WriteLine($"  Description: {details.Description}");
            Console.WriteLine($"  Constraints: {(details.Constraints != null ? string.Join(", ", details.Constraints) : "(none)")}");
            Console.WriteLine($"  JSON Schema: {details.JsonSchema}");
        }
        Console.WriteLine();

        Console.WriteLine("--- JSON Schema for UserCreatedEvent ---");
        var userCreatedDetails = catalog.GetEventDetails("UserCreated");
        if (userCreatedDetails != null)
        {
            Console.WriteLine($"  {userCreatedDetails.JsonSchema}");
        }
    }
}

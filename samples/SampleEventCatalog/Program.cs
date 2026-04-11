using Json.Schema;
using Microsoft.Extensions.DependencyInjection;
using Papst.EventStore.EventCatalog;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SampleEventCatalog;

public class Program
{
    public static async Task Main(string[] args)
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
        var userEvents = await catalog.ListEvents<User>();
        foreach (var entry in userEvents)
        {
            Console.WriteLine($"  Event: {entry.EventName}");
            Console.WriteLine($"    Description: {entry.Description ?? "(none)"}");
            Console.WriteLine($"    Constraints: {(entry.Constraints != null ? string.Join(", ", entry.Constraints) : "(none)")}");
            Console.WriteLine();
        }

        // List all events for Order entity
        Console.WriteLine("--- Events for Order entity ---");
        var orderEvents = await catalog.ListEvents<Order>();
        foreach (var entry in orderEvents)
        {
            Console.WriteLine($"  Event: {entry.EventName}");
            Console.WriteLine($"    Description: {entry.Description ?? "(none)"}");
            Console.WriteLine($"    Constraints: {(entry.Constraints != null ? string.Join(", ", entry.Constraints) : "(none)")}");
            Console.WriteLine();
        }

        // Filter events by constraint
        Console.WriteLine("--- User events with 'Update' constraint ---");
        var updateEvents = await catalog.ListEvents<User>(constraints: new[] { "Update" });
        foreach (var entry in updateEvents)
        {
            Console.WriteLine($"  Event: {entry.EventName} ({entry.Description})");
        }
        Console.WriteLine();

        // Filter events by name
        Console.WriteLine("--- Looking up 'UserCreated' event ---");
        var filtered = await catalog.ListEvents<User>(name: "UserCreated");
        foreach (var entry in filtered)
        {
            Console.WriteLine($"  Found: {entry.EventName} - {entry.Description}");
        }
        Console.WriteLine();

        // Get event details with JSON schema (global lookup)
        Console.WriteLine("--- Event Details with JSON Schema ---");
        var details = await catalog.GetEventDetails("OrderPlaced");
        if (details != null)
        {
            Console.WriteLine($"  Event: {details.EventName}");
            Console.WriteLine($"  Description: {details.Description}");
            Console.WriteLine($"  Constraints: {(details.Constraints != null ? string.Join(", ", details.Constraints) : "(none)")}");
            Console.WriteLine($"  JSON Schema: {details.JsonSchema}");
        }
        Console.WriteLine();

        // Get event details scoped to entity
        Console.WriteLine("--- Entity-scoped Event Details for User/UserCreated ---");
        var userCreatedDetails = await catalog.GetEventDetails<User>("UserCreated");
        if (userCreatedDetails != null)
        {
            Console.WriteLine($"  Event: {userCreatedDetails.EventName}");
            Console.WriteLine($"  Description: {userCreatedDetails.Description}");
            Console.WriteLine($"  JSON Schema: {userCreatedDetails.JsonSchema}");
        }

        Console.WriteLine();

        // --- JSON Schema: build a JSON object and validate it ---
        await DemonstrateSchemaValidationAsync(catalog);
    }

    private static async Task DemonstrateSchemaValidationAsync(IEventCatalog catalog)
    {
        Console.WriteLine("=== JSON Schema Validation Demo ===");
        Console.WriteLine();

        var details = await catalog.GetEventDetails<User>("UserCreated");
        if (details is null)
        {
            Console.WriteLine("  Event 'UserCreated' not found in catalog.");
            return;
        }

        Console.WriteLine($"  Schema for '{details.EventName}':");
        Console.WriteLine($"  {FormatJson(details.JsonSchema)}");
        Console.WriteLine();

        var schema = JsonSchema.FromText(details.JsonSchema);

        // --- Valid object ---
        var validEvent = new JsonObject
        {
            ["name"] = "Alice",
            ["email"] = "alice@example.com"
        };

        var validResult = schema.Evaluate(ToJsonElement(validEvent), new EvaluationOptions { OutputFormat = OutputFormat.List });

        Console.WriteLine("  Valid event object:");
        Console.WriteLine($"  {validEvent.ToJsonString()}");
        Console.WriteLine($"  Validation: {(validResult.IsValid ? "✓ PASSED" : "✗ FAILED")}");
        Console.WriteLine();

        // --- Invalid: wrong type for 'name' ---
        var invalidTypeEvent = new JsonObject
        {
            ["name"] = 42,
            ["email"] = "bob@example.com"
        };

        var invalidTypeResult = schema.Evaluate(ToJsonElement(invalidTypeEvent), new EvaluationOptions { OutputFormat = OutputFormat.List });

        Console.WriteLine("  Invalid event object (wrong type for 'name'):");
        Console.WriteLine($"  {invalidTypeEvent.ToJsonString()}");
        Console.WriteLine($"  Validation: {(invalidTypeResult.IsValid ? "✓ PASSED" : "✗ FAILED")}");
        if (!invalidTypeResult.IsValid)
        {
            foreach (var error in (invalidTypeResult.Details ?? []).Where(d => !d.IsValid && d.Errors is not null))
            {
                foreach (var (key, message) in error.Errors!)
                {
                    Console.WriteLine($"    - [{error.InstanceLocation}] {key}: {message}");
                }
            }
        }
        Console.WriteLine();

        // --- Invalid: extra strictness demo using OrderPlaced ---
        var orderDetails = await catalog.GetEventDetails<Order>("OrderPlaced");
        if (orderDetails is null) return;

        Console.WriteLine($"  Schema for 'OrderPlaced':");
        Console.WriteLine($"  {FormatJson(orderDetails.JsonSchema)}");
        Console.WriteLine();

        var orderSchema = JsonSchema.FromText(orderDetails.JsonSchema);

        // Valid OrderPlaced object
        var validOrder = new JsonObject
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["productName"] = "Widget",
                    ["quantity"] = 2,
                    ["price"] = 9.99
                }
            },
            ["total"] = 19.98
        };

        var validOrderResult = orderSchema.Evaluate(ToJsonElement(validOrder), new EvaluationOptions { OutputFormat = OutputFormat.List });

        Console.WriteLine("  Valid OrderPlaced object:");
        Console.WriteLine($"  {validOrder.ToJsonString()}");
        Console.WriteLine($"  Validation: {(validOrderResult.IsValid ? "✓ PASSED" : "✗ FAILED")}");
        Console.WriteLine();

        // Invalid: total is a string instead of number
        var invalidOrder = new JsonObject
        {
            ["userId"] = Guid.NewGuid().ToString(),
            ["items"] = new JsonArray(),
            ["total"] = "not-a-number"
        };

        var invalidOrderResult = orderSchema.Evaluate(ToJsonElement(invalidOrder), new EvaluationOptions { OutputFormat = OutputFormat.List });

        Console.WriteLine("  Invalid OrderPlaced object ('total' is a string):");
        Console.WriteLine($"  {invalidOrder.ToJsonString()}");
        Console.WriteLine($"  Validation: {(invalidOrderResult.IsValid ? "✓ PASSED" : "✗ FAILED")}");
        if (!invalidOrderResult.IsValid)
        {
            foreach (var error in (invalidOrderResult.Details ?? []).Where(d => !d.IsValid && d.Errors is not null))
            {
                foreach (var (key, message) in error.Errors!)
                {
                    Console.WriteLine($"    - [{error.InstanceLocation}] {key}: {message}");
                }
            }
        }
    }

    private static JsonElement ToJsonElement(JsonNode node)
      => JsonSerializer.Deserialize<JsonElement>(node.ToJsonString());

    private static string FormatJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}

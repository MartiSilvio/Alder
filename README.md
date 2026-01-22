# CsEval

CsEval is a runtime expression evaluator for .NET 8. It takes a string containing C#-like code and executes it, returning the result. Think of it as `eval()` for C# - but safe, extensible, and designed for embedding in applications.

```csharp
var engine = new CsEvalEngine();
var result = engine.Evaluate("1 + 2 * 3"); // 7
```

## What It Does

CsEval parses and evaluates expressions at runtime. You give it a string, it gives you back a value. Use it for:

- **Dynamic queries** - Build data retrieval logic that can be modified without recompilation
- **Rule engines** - Define business rules as expressions that non-developers can edit
- **Calculated fields** - Let users define formulas like `Price * Quantity * (1 - Discount)`
- **Scripting** - Add scripting capabilities to your application

## Installation

```xml
<ProjectReference Include="path/to/CsEval.csproj" />
```

## Quick Start

```csharp
using CsEval;

var engine = new CsEvalEngine();

// Basic math
engine.Evaluate("10 + 5 * 2");                    // 20

// Variables
engine.SetVariable("price", 100);
engine.SetVariable("qty", 3);
engine.Evaluate("price * qty");                   // 300

// Strings
engine.SetVariable("name", "World");
engine.Evaluate("$\"Hello, {name}!\"");           // "Hello, World!"

// LINQ on collections
engine.SetVariable("orders", orderList);
engine.Evaluate("orders.Where(x => x.Total > 100).Sum(x => x.Total)");

// Objects
engine.Evaluate("new { Name = \"John\", Age = 30 }");
```

## Standout Features

### Block Expressions with Control Flow

Go beyond simple formulas. Write multi-statement logic with variables, conditionals, and early returns:

```csharp
var query = @"{
    var item = Data.GetById(id);
    if (item == null) return null;

    if (item.Status == ""archived"") {
        return new { Error = ""Item is archived"" };
    }

    return item;
}";

engine.Evaluate(query);
```

### Object Merging

The `+` operator combines objects - add computed properties to existing data:

```csharp
// Original entity + computed fields
entity + new {
    FullName = entity.FirstName + " " + entity.LastName,
    IsExpired = entity.ExpiryDate < DateTime.Now
}
// Result: all original properties plus FullName and IsExpired
```

### Dependency Injection Integration

Modules resolve from `IServiceProvider` at evaluation time - full DI support:

```csharp
[CsEvalModule("Members")]
public class MemberModule
{
    private readonly IDbContext _db;
    public MemberModule(IDbContext db) => _db = db;
    public Member? GetById(int id) => _db.Members.Find(id);
}

// Register in DI
services.AddScoped<MemberModule>();
engine.RegisterFromAssembly(typeof(MemberModule).Assembly);

// Module instance created via DI when expression runs
engine.Evaluate("Members.GetById(123)", serviceProvider);
```

### Async with Cancellation

Long-running expressions can be cancelled:

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await engine.EvaluateAsync(expression, serviceProvider, cts.Token);
```

## Core Capabilities

### Expressions

```csharp
// Arithmetic: +, -, *, /, %
// Comparison: ==, !=, <, <=, >, >=
// Logical: &&, ||, !
// Ternary: condition ? a : b
// Null handling: ??, ?., ??=
```

### LINQ

```csharp
items.Where(x => x.Active)
items.Select(x => x.Name)
items.OrderBy(x => x.Date).Take(10)
items.Sum(x => x.Value)
items.Any(x => x.Status == "pending")
items.Aggregate(0, (sum, x) => sum + x.Value)
```

### Built-in Modules

```csharp
Math.Round(3.14159, 2)          // 3.14
DateTime.Now                     // current time
Guid.NewGuid()                   // new GUID
String.IsNullOrEmpty(s)          // null check
Convert.ToInt32(value)           // conversion
Enumerable.Range(0, 10)          // sequences
```

### Extensibility

```csharp
// Custom functions
engine.RegisterFunction("double", args => (long)args[0] * 2);

// Custom modules
engine.RegisterModule("Cache", new CacheService());

// Attribute-based registration
[CsEvalModule("Data")]
public class DataModule { ... }
engine.RegisterFromAssembly(assembly);
```

## Configuration

```csharp
// Case-insensitive identifiers
var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });

// Pre-parse for repeated evaluation
var expr = engine.Parse("items.Sum(x => x.Value)");
engine.Evaluate(expr); // faster

// Isolated child contexts
var child = engine.CreateChild();
child.SetVariable("requestId", 123); // parent can't see this
```

## Documentation

- [Syntax Reference](docs/syntax.md) - Grammar, operators, language constructs
- [Features Guide](docs/features.md) - LINQ methods, built-in modules, extensibility
- [API Reference](docs/api.md) - Complete API documentation

## License

MIT

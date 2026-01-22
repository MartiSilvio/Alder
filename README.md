# CsEval

[![.NET](https://github.com/MartiSilvio/CsEval/blob/master/.github/workflows/dotnet.yml/badge.svg)](https://github.com/MartiSilvio/CsEval/blob/master/.github/workflows/dotnet.yml)

**A C# expression evaluator and dynamic expression parser for .NET**

CsEval is a runtime expression evaluation library that parses and executes C#-like expressions from strings. It provides formula evaluation, dynamic query building, and scripting capabilities for .NET applications.

```csharp
var engine = new CsEvalEngine();
engine.Evaluate("1 + 2 * 3"); // 7
```

## Use Cases

- **Formula evaluation** - Calculate `Price * Quantity * (1 - Discount)` at runtime
- **Dynamic queries** - Build data retrieval expressions without recompilation
- **Rule engines** - Define business rules as expressions that can be modified on the fly
- **Scripting** - Add expression-based scripting to your application
- **Calculated fields** - User-defined formulas for reports and dashboards

## Installation

```xml
<ProjectReference Include="path/to/CsEval.csproj" />
```

## Quick Start

```csharp
using CsEval;

var engine = new CsEvalEngine();

// Arithmetic expressions
engine.Evaluate("10 + 5 * 2");                    // 20

// Variables
engine.SetVariable("price", 100);
engine.SetVariable("qty", 3);
engine.Evaluate("price * qty");                   // 300

// String interpolation
engine.SetVariable("name", "World");
engine.Evaluate("$\"Hello, {name}!\"");           // "Hello, World!"

// LINQ expressions
engine.SetVariable("orders", orderList);
engine.Evaluate("orders.Where(x => x.Total > 100).Sum(x => x.Total)");

// Anonymous objects
engine.Evaluate("new { Name = \"John\", Age = 30 }");
```

## Key Features

### Expression Syntax

C#-like syntax with arithmetic, comparison, logical, and null-handling operators:

```csharp
// Arithmetic: +, -, *, /, %
// Comparison: ==, !=, <, <=, >, >=
// Logical: &&, ||, !
// Ternary: condition ? a : b
// Null-safe: ??, ?., ??=
```

### Lambda Expressions & LINQ

Full lambda support for querying collections:

```csharp
items.Where(x => x.Active)
items.Select(x => x.Name)
items.OrderBy(x => x.Date).Take(10)
items.Sum(x => x.Value)
items.Any(x => x.Status == "pending")
items.Aggregate(0, (sum, x) => sum + x.Value)
```

### Control Flow

Multi-statement expressions with variables, conditionals, and early returns:

```csharp
{
    var item = Data.GetById(id);
    if (item == null) return null;

    if (item.Status == "archived") {
        return new { Error = "Item is archived" };
    }

    return item;
}
```

### Object Merging

Combine objects with the `+` operator - add computed properties to existing data:

```csharp
entity + new {
    FullName = entity.FirstName + " " + entity.LastName,
    IsExpired = entity.ExpiryDate < DateTime.Now
}
```

### Built-in Functions

Standard library modules included:

```csharp
Math.Round(3.14159, 2)          // 3.14
DateTime.Now                     // current time
Guid.NewGuid()                   // new GUID
String.IsNullOrEmpty(s)          // null check
Convert.ToInt32(value)           // type conversion
Enumerable.Range(0, 10)          // sequence generation
```

### Extensibility

Register custom functions and modules:

```csharp
// Custom functions
engine.RegisterFunction("twice", args => (long)args[0] * 2);

// Custom modules
engine.RegisterModule("Cache", new CacheService());

// Attribute-based registration with dependency injection
[CsEvalModule("Data")]
public class DataModule
{
    private readonly IDbContext _db;
    public DataModule(IDbContext db) => _db = db;
    public object GetById(int id) => _db.Find(id);
}

services.AddScoped<DataModule>();
engine.RegisterFromAssembly(typeof(DataModule).Assembly);
engine.Evaluate("Data.GetById(123)", serviceProvider);
```

### Async & Cancellation

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
await engine.EvaluateAsync(expression, serviceProvider, cts.Token);
```

## Configuration

```csharp
// Case-insensitive evaluation
var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });

// Pre-parse for performance (faster repeated evaluation)
var expr = engine.Parse("items.Sum(x => x.Value)");
engine.Evaluate(expr);

// Child contexts with isolated scope
var child = engine.CreateChild();
child.SetVariable("requestId", 123);
```

## Documentation

- [Syntax Reference](docs/syntax.md) - Expression grammar and language constructs
- [Features Guide](docs/features.md) - LINQ methods, built-in modules, extensibility
- [API Reference](docs/api.md) - Complete API documentation

## License

[MIT](LICENSE)

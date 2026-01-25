# CsEval

[![.NET](https://github.com/MartiSilvio/CsEval/actions/workflows/dotnet.yml/badge.svg)](https://github.com/MartiSilvio/CsEval/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 7](https://img.shields.io/badge/.NET-7.0-purple.svg)](https://dotnet.microsoft.com/)

## A zero-dependency C# expression evaluator for .NET

CsEval is a **lightweight expression evaluator** that parses C#-like syntax at runtime with zero external dependencies. Designed for **rule engines**, **dynamic filters**, **calculated fields**, **formula evaluation**, and more, it supports LINQ with lambdas, control flow, object merging, and more — all in a sandboxed environment.

```csharp
var engine = new CsEvalEngine();
engine.Evaluate("1 + 2 * 3");  // 7
engine.Evaluate("items.Where(x => x.Price > 100).Sum(x => x.Price)");
```

With CsEval, you can:

- ✅ Evaluate arithmetic, logical, and string expressions dynamically
- ✅ Run full **LINQ queries** with lambda expressions on your collections
- ✅ Use **control flow** — loops, conditionals, multi-statement blocks, early returns
- ✅ Dynamically **merge objects** and extend data structures on the fly
- ✅ **Sandbox** expressions with configurable security modes
- ✅ Execute in **thread-safe**, isolated evaluation contexts

---

## Why CsEval?

CsEval goes beyond simple expression evaluation. It supports features that enable **real programming logic at runtime**:

| Feature                    | Description                                                       |
| -------------------------- | ----------------------------------------------------------------- |
| **Zero Dependencies**      | No external NuGet packages — just the .NET SDK                    |
| **Full LINQ with Lambdas** | `items.Where(x => x.Active).Sum(x => x.Value)`                    |
| **All C# Loops**           | `while`, `for`, `foreach`, `do-while` with `break` and `continue` |
| **Block Expressions**      | Variables, conditionals, and early returns                        |
| **Object Merging**         | `entity + new { Computed = value }` to extend data on the fly     |
| **Thread-Safe Contexts**   | Isolated child contexts for parallel evaluation                   |
| **Dependency Injection**   | Resolve modules from `IServiceProvider` at evaluation time        |

---

## Installation

```xml
<PackageReference Include="CsEval" Version="1.0.0" />
```

---

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

---

## Documentation

| Guide                                    | Description                                                   |
| ---------------------------------------- | ------------------------------------------------------------- |
| [**Syntax Reference**](docs/syntax.md)   | Expression grammar, operators, and language constructs        |
| [**Features Guide**](docs/features.md)   | LINQ methods, loops, assignment, built-in modules             |
| [**Extensions**](docs/extensions.md)     | Object merging, spread operator, and CsEval-specific features |
| [**Sandbox Modes**](docs/sandbox.md)     | Security modes and reflection blocking                        |
| [**API Reference**](docs/api.md)         | Complete public API documentation                             |
| [**Architecture**](docs/architecture.md) | Internal design and implementation details                    |

---

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
     .OrderBy(x => x.Date)
     .Select(x => x.Name)
     .Take(10)

items.Sum(x => x.Value)
items.Any(x => x.Status == "pending")
items.GroupBy(x => x.Category)
items.Aggregate(0, (sum, x) => sum + x.Value)
```

### All C# Loops

Full loop support:

```csharp
{
    var sum = 0;
    foreach (var item in items) {
        if (item.Skip) continue;
        if (item.Value > 1000) break;
        sum = sum + item.Value;
    }
    return sum;
}

// Also: while, for, do-while
for (var i = 0; i < 10; i++) { ... }
while (condition) { ... }
do { ... } while (condition);
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

Combine objects with the `+` operator — add computed properties to existing data:

```csharp
entity + new {
    FullName = entity.FirstName + " " + entity.LastName,
    IsExpired = entity.ExpiryDate < DateTime.Now
}

// Spread operator
[...existingItems, newItem]
new { ...baseConfig, Override = "value" }
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

### Cancellation

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
engine.Evaluate(expression, serviceProvider, cts.Token);
```

---

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

---

## Thread Safety

**Important**: `CsEvalEngine` instances are **not thread-safe** for concurrent evaluation. The evaluation context maintains mutable state that can be corrupted by simultaneous access.

For concurrent scenarios, use `CreateChild()` to create isolated contexts:

```csharp
var engine = new CsEvalEngine();
engine.SetVariable("config", sharedConfig); // shared setup

// Concurrent evaluation - each thread gets its own context
Parallel.ForEach(items, item => {
    var child = engine.CreateChild();        // isolated context
    child.SetVariable("item", item);         // thread-local variable
    var result = child.Evaluate(expression);
});
```

Each child context:

- Inherits variables from the parent (read-only)
- Has its own isolated scope for new variables
- Can be safely used from a single thread

---

## Additional Features

**JavaScript-friendly syntax** — `let`, `undefined`, `===`/`!==`, plus method aliases: `filter`, `map`, `reduce`, `find`, `some`, `every`, `includes`.

**Sandbox Modes** — `Trusted`, `Safe`, and `Strict` presets with granular overrides for secure evaluation.

**Expression Compilation** — Optional compilation to delegates (via Expression Trees) for maximum performance on repeated evaluations.

---

## License

[MIT](LICENSE)

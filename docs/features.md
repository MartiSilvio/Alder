# CsEval Features Guide

This document covers the complete feature set of CsEval including LINQ methods, built-in modules, and extensibility options.

## LINQ Methods

All methods work on any `IEnumerable`. Results are `List<object?>` (see [architecture.md](architecture.md) for rationale).

### Filtering & Projection

```csharp
items.Where(x => x > 0)              // Filter by predicate
items.Select(x => x.Name)            // Project to new shape
items.SelectMany(x => x.Tags)        // Flatten nested collections
items.Distinct()                     // Remove duplicates
items.Take(5)                        // First n elements
items.Skip(10)                       // Skip first n elements
```

### Ordering

```csharp
items.OrderBy(x => x.Date)           // Ascending order
items.OrderByDescending(x => x.Value) // Descending order
items.Reverse()                      // Reverse order
```

### Element Access

```csharp
items.First()                        // First element (throws if empty)
items.First(x => x.Active)           // First matching predicate
items.FirstOrDefault()               // First or null
items.FirstOrDefault(x => x.Active)  // First matching or null
items.Last()                         // Last element
items.Last(x => x.Active)            // Last matching predicate
items.LastOrDefault()                // Last or null
items.LastOrDefault(x => x.Active)   // Last matching or null
items.Single()                       // Single element (throws if != 1)
items.Single(x => x.Id == id)        // Single matching
items.SingleOrDefault()              // Single or null
items.SingleOrDefault(x => x.Id == id)
```

### Aggregation

```csharp
items.Count()                        // Number of elements
items.Count(x => x.Active)           // Number matching predicate
items.Sum()                          // Sum of numeric collection (throws for non-numeric)
items.Sum(x => x.Value)              // Sum of selected values (selector must return numeric)
items.Average()                      // Average of numeric collection (throws for non-numeric)
items.Average(x => x.Value)          // Average of selected values
items.Min()                          // Minimum value
items.Min(x => x.Value)              // Minimum of selected values
items.Max()                          // Maximum value
items.Max(x => x.Value)              // Maximum of selected values
items.MinBy(x => x.Date)             // Element with minimum key
items.MaxBy(x => x.Date)             // Element with maximum key
items.Aggregate((acc, x) => acc + x) // Reduce without seed
items.Aggregate(0, (acc, x) => acc + x) // Reduce with seed
```

### Predicates

```csharp
items.Any()                          // True if any elements
items.Any(x => x.Active)             // True if any match
items.All(x => x.Valid)              // True if all match
items.Contains(value)                // True if contains value
// Or use Python-style 'in' operator (see extensions.md)
// value in items                   // Same as items.Contains(value)
```

### Grouping

```csharp
// GroupBy returns List<Dictionary> with "Key" and "Items" properties
items.GroupBy(x => x.Category)
// Result: [{ Key: "A", Items: [...] }, { Key: "B", Items: [...] }]

// Access grouped data
var groups = items.GroupBy(x => x.Status)
groups.First().Key                   // Get group key
groups.First().Items                 // Get items in group
```

### Combination

```csharp
items.Concat(otherItems)             // Combine two sequences

// Zip with selector
names.Zip(ages, (n, a) => n + ": " + a)  // ["Alice: 30", "Bob: 25"]

// Zip without selector returns dictionaries with First/Second
names.Zip(ages)                      // [{ First: "Alice", Second: 30 }, ...]
```

### Set Operations

```csharp
first.Except(second)                 // Elements in first but not in second
first.Intersect(second)              // Elements in both collections
first.Union(second)                  // All elements, no duplicates
```

### Conversion

```csharp
items.ToList()                       // Convert to List<object?>
items.ToArray()                      // Convert to object?[]
```

### JavaScript Method Aliases

For JavaScript/TypeScript developers, familiar method names work as aliases:

| JavaScript | LINQ Equivalent  | Notes                                 |
| ---------- | ---------------- | ------------------------------------- |
| `filter`   | `Where`          | Same behavior                         |
| `map`      | `Select`         | Same behavior                         |
| `flatMap`  | `SelectMany`     | Same behavior                         |
| `reduce`   | `Aggregate`      | JS argument order: `reduce(fn, seed)` |
| `find`     | `FirstOrDefault` | Same behavior                         |
| `some`     | `Any`            | Same behavior                         |
| `every`    | `All`            | Same behavior                         |
| `includes` | `Contains`       | Same behavior                         |

```csharp
// These are equivalent
items.Where(x => x.Active)
items.filter(x => x.Active)

// reduce uses JS argument order (function first, seed second)
items.reduce((acc, x) => acc + x, 0)      // JS style
items.Aggregate(0, (acc, x) => acc + x)   // C# style
```

## Index & Property Assignment

For basic assignment, compound assignment (`+=`, `-=`, etc.), and increment/decrement operators, see [syntax.md](syntax.md#assignment).

### Index Assignment

Set values in arrays, lists, and dictionaries:

```csharp
// Array/List index assignment
{
    var arr = [1, 2, 3];
    arr[1] = 99;
    return arr[1];    // 99
}

// Dictionary index assignment
{
    var dict = new { key = "old" };
    dict["key"] = "new";
    dict["newKey"] = "added";  // Add new key
    return dict["key"];        // "new"
}

// Modify external collections
var list = new List<object?> { 1, 2, 3 };
engine.SetVariable("items", list);
engine.Evaluate("items[0] = 100");  // list[0] is now 100
```

### Property Assignment

Set properties on objects:

```csharp
// Anonymous object property assignment
{
    var obj = new { Name = "John", Age = 25 };
    obj.Name = "Jane";
    obj.City = "NYC";  // Add new property
    return obj.Name;   // "Jane"
}

// Nested property assignment
{
    var obj = new { Inner = new { Value = 10 } };
    obj.Inner.Value = 99;
    return obj.Inner.Value;  // 99
}

// Modify external typed objects
var person = new Person { Name = "John", Age = 25 };
engine.SetVariable("person", person);
engine.Evaluate("person.Name = \"Jane\"");  // person.Name is now "Jane"
```

Both index and property assignment return the assigned value, enabling chained expressions:

```csharp
{
    var arr = [1, 2, 3];
    var x = arr[0] = 100;  // x = 100, arr[0] = 100
    return x;
}
```

## Loop Safety

For loop syntax (`while`, `for`, `foreach`, `do-while`, `break`, `continue`, `switch`), see [syntax.md](syntax.md#loops).

All loops have a configurable iteration limit to prevent infinite loops:

```csharp
// Default: 100,000 iterations max
var engine = new CsEvalEngine();

// Custom limit
var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 1000 });

// Disable limit (use with caution)
var engine = new CsEvalEngine(new CsEvalOptions { MaxIterations = 0 });
```

Exceeding the limit throws an `EvalException`.

## Built-in Modules

### Math

Mathematical functions and constants.

```csharp
// Rounding
Math.Abs(-5)                         // 5
Math.Floor(3.7)                      // 3
Math.Ceiling(3.2)                    // 4
Math.Round(3.5)                      // 4
Math.Round(3.14159, 2)               // 3.14

// Comparison
Math.Min(a, b)                       // Smaller of two
Math.Max(a, b)                       // Larger of two

// Powers & Roots
Math.Pow(2, 10)                      // 1024
Math.Sqrt(16)                        // 4
Math.Exp(1)                          // e^1

// Logarithms
Math.Log(x)                          // Natural log
Math.Log10(x)                        // Base-10 log

// Trigonometry
Math.Sin(x)
Math.Cos(x)
Math.Tan(x)

// Constants
Math.PI                              // 3.14159...
Math.E                               // 2.71828...
```

### DateTime

Date and time operations.

```csharp
// Current time
DateTime.Now                         // Local time
DateTime.UtcNow                      // UTC time
DateTime.Today                       // Today at midnight

// Limits
DateTime.MinValue
DateTime.MaxValue

// Parsing
DateTime.Parse("2024-01-15")
DateTime.TryParse("2024-01-15", out result)
```

### Guid

Unique identifier operations.

```csharp
Guid.NewGuid()                       // Generate new GUID
Guid.Empty                           // 00000000-0000-0000-0000-000000000000
Guid.Parse("...")                    // Parse from string
Guid.TryParse("...", out result)
```

### Convert

Type conversion utilities.

```csharp
Convert.ToInt32(value)
Convert.ToInt64(value)
Convert.ToDouble(value)
Convert.ToBoolean(value)
Convert.ToString(value)
Convert.ToDecimal(value)
```

### String

String utilities.

```csharp
String.Empty                         // ""
String.IsNullOrEmpty(s)              // True if null or ""
String.IsNullOrWhiteSpace(s)         // True if null, "", or whitespace
String.Join(", ", items)             // Join with separator
String.Concat(a, b, c)               // Concatenate
String.Format("{0} - {1}", a, b)     // Format string
```

### Enumerable

Sequence generation.

```csharp
Enumerable.Range(0, 10)              // [0, 1, 2, ..., 9]
Enumerable.Repeat("x", 5)            // ["x", "x", "x", "x", "x"]
Enumerable.Empty()                   // Empty sequence
```

## Extensibility

### Custom Functions

Register simple functions that take `object?[]` and return `object?`:

```csharp
var engine = new CsEvalEngine();

engine.RegisterFunction("twice", args => (long)args[0] * 2);
engine.RegisterFunction("greet", args => $"Hello, {args[0]}!");
engine.RegisterFunction("clamp", args => {
    var value = Convert.ToDouble(args[0]);
    var min = Convert.ToDouble(args[1]);
    var max = Convert.ToDouble(args[2]);
    return Math.Clamp(value, min, max);
});

engine.Evaluate("twice(5)");        // 10
engine.Evaluate("greet(\"World\")"); // "Hello, World!"
engine.Evaluate("clamp(150, 0, 100)"); // 100
```

### Custom Modules

Register a class as a module with a namespace:

```csharp
public class DataService
{
    public object GetUser(string id) { ... }
    public IEnumerable<object> GetAllUsers() { ... }
    public int Count() { ... }
}

var engine = new CsEvalEngine();
engine.RegisterModule("Users", new DataService());

engine.Evaluate("Users.GetUser(\"123\")");
engine.Evaluate("Users.GetAllUsers().Where(u => u.Active)");
engine.Evaluate("Users.Count()");
```

### Attribute-Based Registration

Use attributes for automatic discovery:

```csharp
[CsEvalModule("Data")]
public class DataModule
{
    public object GetById(string id) => ...;
    public IEnumerable<object> GetAll() => ...;
}

[CsEvalModule("Cache")]
public class CacheModule
{
    public object Get(string key) => ...;
    public void Set(string key, object value) { ... }
}

// Register all modules from assembly
var engine = new CsEvalEngine();
engine.RegisterFromAssembly(typeof(DataModule).Assembly);

engine.Evaluate("Data.GetById(\"123\")");
engine.Evaluate("Cache.Get(\"user:123\")");
```

### Global Functions with Attributes

Register methods as global functions (no module prefix):

```csharp
public class GlobalFunctions
{
    [CsEvalFunction("sum")]
    public long Sum(long a, long b) => a + b;

    [CsEvalFunction("format")]
    public string Format(string template, params object[] args)
        => string.Format(template, args);

    [CsEvalFunction("now")]
    public DateTime Now() => DateTime.Now;
}

var engine = new CsEvalEngine();
engine.RegisterFromType<GlobalFunctions>();

engine.Evaluate("sum(10, 20)");      // 30
engine.Evaluate("format(\"{0} items\", 5)"); // "5 items"
engine.Evaluate("now()");            // Current DateTime
```

### Dependency Injection Integration

Modules can be resolved from `IServiceProvider` at evaluation time:

```csharp
// Define module with dependencies
[CsEvalModule("Members")]
public class MemberModule
{
    private readonly IDbContext _db;
    private readonly ILogger _logger;

    public MemberModule(IDbContext db, ILogger<MemberModule> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Member? GetById(int id)
    {
        _logger.LogInformation("Fetching member {Id}", id);
        return _db.Members.Find(id);
    }

    public IEnumerable<Member> GetActive()
        => _db.Members.Where(m => m.IsActive);
}

// Register in DI container
services.AddScoped<MemberModule>();

// Register module type (not instance)
var engine = new CsEvalEngine();
engine.RegisterFromAssembly(typeof(MemberModule).Assembly);

// Pass serviceProvider at evaluation time
var result = engine.Evaluate("Members.GetById(123)", serviceProvider);
```

## Configuration

### Case Sensitivity

```csharp
// Case-sensitive (default)
var engine = new CsEvalEngine();
engine.SetVariable("MyVar", 42);
engine.Evaluate("MyVar");  // OK
engine.Evaluate("myvar");  // Throws EvalException

// Case-insensitive
var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });
engine.SetVariable("MyVar", 42);
engine.Evaluate("MyVar");  // OK
engine.Evaluate("myvar");  // OK
engine.Evaluate("MYVAR");  // OK
```

### Pre-parsing for Performance

Parse once, evaluate many times:

```csharp
var engine = new CsEvalEngine();
var expression = engine.Parse("items.Where(x => x.Active).Sum(x => x.Value)");

// Faster for repeated evaluation (avoids re-parsing)
foreach (var dataset in datasets)
{
    engine.SetVariable("items", dataset);
    var result = engine.Evaluate(expression);
}
```

### Automatic IL Compilation

CsEval compiles expressions to native IL via `System.Linq.Expressions` (Expression Trees) for maximum performance. All expressions are automatically compiled during `Parse()` with silent fallback to tree-walking for non-compilable expressions.

```csharp
var engine = new CsEvalEngine();
engine.SetVariable("x", 10);
engine.SetVariable("y", 20);

var expr = engine.Parse("x + y * 2");  // Automatically IL-compiled
Console.WriteLine(expr.IsCompiled);    // true

engine.Evaluate(expr);  // Uses compiled delegate for maximum performance
```

**What compiles to native IL:**

- Literals, identifiers, property access
- Arithmetic (`+`, `-`, `*`, `/`, `%`)
- Comparisons (`==`, `!=`, `<`, `>`, `<=`, `>=`)
- Logical (`&&`, `||`, `!`) with short-circuit
- Ternary (`? :`), null coalesce (`??`)
- Blocks with multiple statements
- Control flow: `if`/`else`, `for`, `while`, `do-while`, `foreach`
- `break`, `continue`, `return` (uses IL branches, not exceptions)
- Variable declarations and assignments
- Compound assignments (`+=`, `-=`, etc.) and increment/decrement

**Key performance benefits:**

- Loops use native IL branch instructions instead of exception-based control flow
- Variables in loops use IL local slots instead of dictionary lookups
- `break`/`continue` use `br` opcodes instead of throwing exceptions

**What falls back to tree-walking:**

- LINQ methods with lambda expressions
- Method calls on objects
- Switch statements
- Object spread/merge

### Child Contexts

Create isolated contexts that inherit parent variables:

```csharp
var parent = new CsEvalEngine();
parent.SetVariable("shared", 100);
parent.RegisterModule("Data", new DataModule());

// Each request gets isolated context
foreach (var request in requests)
{
    var child = parent.CreateChild();
    child.SetVariable("requestId", request.Id);
    child.SetVariable("userId", request.UserId);

    var result = child.Evaluate(request.Expression);
    // child can access: shared, Data, requestId, userId
    // parent cannot access: requestId, userId
}
```

### Cancellation Support

```csharp
var cts = new CancellationTokenSource();
cts.CancelAfter(TimeSpan.FromSeconds(5));

try
{
    var result = engine.Evaluate(expression, serviceProvider, cts.Token);
}
catch (OperationCanceledException)
{
    // Evaluation was cancelled
}
```

## Error Handling

### Parse Errors

```csharp
// Using TryParse
if (engine.TryParse(expression, out var parsed, out var error))
{
    var result = engine.Evaluate(parsed);
}
else
{
    Console.WriteLine($"Parse error: {error}");
}

// Using exception handling
try
{
    var parsed = engine.Parse(expression);
}
catch (ParserException ex)
{
    // Syntax error in expression
}
catch (LexerException ex)
{
    // Tokenization error
}
```

### Evaluation Errors

```csharp
try
{
    var result = engine.Evaluate(expression);
}
catch (EvalException ex)
{
    // Runtime error (null access, type mismatch, etc.)
}
```

## Type Coercion

CsEval automatically coerces types in common scenarios:

### Numbers

CsEval matches C# numeric literal behavior:

- `42` → `int` (default for integers that fit in int range)
- `2147483648` → `long` (auto-promotes if too large for int)
- `42L` → `long` (explicit suffix)
- `3.14` → `double` (default for floating-point)
- `3.14f` → `float` (explicit suffix)
- `3.14m` → `decimal` (explicit suffix)

```csharp
engine.Evaluate("42");       // int
engine.Evaluate("42L");      // long
engine.Evaluate("3.14");     // double
engine.Evaluate("3.14f");    // float
engine.Evaluate("3.14m");    // decimal
```

Arithmetic follows C# promotion rules exactly (via `dynamic`):

- Small types (`byte`, `short`, etc.) promote to `int`
- `int + int` → `int`
- `int / int` → `int` (truncating! Use `5.0 / 2.0` for fractional results)
- `int + long` → `long`
- `int + float` → `float`
- `float + double` → `double`
- `int + decimal` → `decimal`
- `decimal + float` or `decimal + double` → **Throws!** (C# forbids mixing these)

### Strings

CsEval supports multiple string literal formats:

```csharp
// Regular strings (escape sequences supported)
"hello\nworld"                    // Newline escape
"path\\to\\file"                  // Escaped backslash

// Interpolated strings
$"Hello, {name}!"                 // Variable interpolation
$"Sum: {a + b}"                   // Expression interpolation
$"Braces: {{literal}}"            // Escaped braces

// Verbatim strings (backslashes literal)
@"C:\Users\John"                  // No escaping needed
@"She said ""Hello"""             // Double quotes to escape

// Verbatim interpolated strings (both features)
$@"Path: {user}\Documents"        // Verbatim + interpolation
@$"C:\{folder}\file.txt"          // Either prefix order works
$@"Literal: {{braces}}"           // Escaped braces still work
```

### Nullables

Null can be used with any reference type or nullable value type.

### Method Parameters

When calling methods, CsEval:

1. Checks for exact type match
2. Attempts `Convert.ChangeType` for compatible types
3. Uses default values for missing optional parameters
4. Auto-appends `CancellationToken` if method expects it

### Named Parameters

Methods can be called with named parameters using `name: value` syntax:

```csharp
// Specify parameters by name
str.Substring(startIndex: 0, length: 5)

// Parameters can be in any order when named
Math.Round(digits: 2, value: 3.14159)

// Mix positional and named (positional must come first)
str.Substring(0, length: 5)

// Skip optional parameters - they use default values
engine.RegisterModule("name", type, explicitOnly: true)
```

Named parameters:

- Match by parameter name (case-insensitive)
- Can appear in any order after positional arguments
- Allow skipping optional parameters with defaults
- Work with both module methods and object instance methods

## Tips

1. **Numeric literals match C#**: `42` is `int`, `42L` is `long`, `3.14` is `double`, `3.14m` is `decimal`. Large integers auto-promote to `long`. Arithmetic follows C# type promotion rules exactly. **Important**: `5/2` returns `2` (truncating), use `5.0/2.0` for `2.5`. Mixing `decimal` with `float`/`double` throws.

2. **Object merging with null**: `null + dict` throws. Use null checks or `??`.

3. **Block scope**: Variables declared with `var` or typed (`int x = 5;`) are scoped to the block. Type keywords (`int`, `long`, etc.) are reserved (matching C# behavior).

4. **Case sensitivity**: Default is case-sensitive. Use `CsEvalOptions { IgnoreCase = true }` for case-insensitive.

5. **Service resolution timing**: `IServiceProvider` is used at evaluation time, not registration time.

6. **Lambda body is single expression**: No blocks in lambda body, use ternary for conditionals.

7. **GroupBy returns dictionaries**: Results have `Key` and `Items` properties, not C#'s `IGrouping<TKey, TElement>`.

8. **Zip without selector**: Returns dictionaries with `First` and `Second` properties, not tuples.

9. **Loop iteration limit**: Loops have a default limit of 100,000 iterations to prevent infinite loops. Configure via `CsEvalOptions.MaxIterations`.

10. **Break/continue scope**: `break` and `continue` only affect the innermost loop.

11. **LINQ aggregation types**: `Sum()` and `Average()` require numeric collections. Calling `Sum()` on strings or non-numeric types throws `InvalidOperationException`.

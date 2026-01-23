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
items.Sum()                          // Sum of numeric collection
items.Sum(x => x.Value)              // Sum of selected values
items.Average()                      // Average of numeric collection
items.Average(x => x.Value)          // Average of selected values
items.Min()                          // Minimum value
items.Min(x => x.Value)              // Minimum of selected values
items.Max()                          // Maximum value
items.Max(x => x.Value)              // Maximum of selected values
items.Aggregate((acc, x) => acc + x) // Reduce without seed
items.Aggregate(0, (acc, x) => acc + x) // Reduce with seed
```

### Predicates

```csharp
items.Any()                          // True if any elements
items.Any(x => x.Active)             // True if any match
items.All(x => x.Valid)              // True if all match
items.Contains(value)                // True if contains value
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

### Conversion

```csharp
items.ToList()                       // Convert to List<object?>
items.ToArray()                      // Convert to object?[]
```

## Assignment

### Basic Assignment

```csharp
{
    var x = 10;
    x = 20;       // Reassign value
    return x;     // 20
}
```

### Compound Assignment

All 10 compound assignment operators are supported:

```csharp
// Arithmetic
x += 5;      // x = x + 5
x -= 3;      // x = x - 3
x *= 2;      // x = x * 2
x /= 4;      // x = x / 4
x %= 3;      // x = x % 3

// Bitwise
x &= mask;   // x = x & mask
x |= flags;  // x = x | flags
x ^= bits;   // x = x ^ bits
x <<= 2;     // x = x << 2
x >>= 1;     // x = x >> 1
```

Compound assignment works with:
- **Integers**: `int`, `long`, `byte`, `short`, etc.
- **Floating-point**: `double`, `float`, `decimal`
- **Strings**: `+=` concatenates strings

```csharp
{
    var s = "Hello";
    s += " World";
    return s;     // "Hello World"
}
```

### Increment/Decrement

Both prefix and postfix increment/decrement operators are supported:

```csharp
// Prefix: modify, then return new value
var x = 5;
var a = ++x;    // a = 6, x = 6
var b = --x;    // b = 5, x = 5

// Postfix: return old value, then modify
var y = 10;
var c = y++;    // c = 10, y = 11
var d = y--;    // d = 11, y = 10
```

Commonly used in loops:

```csharp
{
    var sum = 0;
    for (var i = 0; i < 5; i++) {
        sum += i;
    }
    return sum;    // 10
}
```

## Loops

CsEval supports all C# loop types with `break` and `continue` for flow control.

### While Loop

```csharp
{
    var sum = 0;
    var i = 0;
    while (i < 10) {
        sum += i;
        i += 1;
    }
    return sum;   // 45
}
```

### For Loop

```csharp
{
    var sum = 0;
    for (var i = 0; i < 10; i += 1) {
        sum += i;
    }
    return sum;   // 45
}
```

### Foreach Loop

```csharp
{
    var sum = 0;
    foreach (var item in items) {
        sum += item.Value;
    }
    return sum;
}
```

### Do-While Loop

```csharp
{
    var i = 0;
    do {
        i += 1;
    } while (i < 5);
    return i;     // 5
}
```

### Break and Continue

```csharp
// Find first matching element
{
    var result = -1;
    foreach (var item in items) {
        if (item.Match) {
            result = item.Id;
            break;           // Exit loop early
        }
    }
    return result;
}

// Skip certain elements
{
    var sum = 0;
    for (var i = 0; i < 10; i += 1) {
        if (i % 2 == 0) continue;  // Skip even numbers
        sum += i;
    }
    return sum;   // 25 (1+3+5+7+9)
}
```

### Loop Safety

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

### Expression Compilation

For maximum performance with simple expressions, enable compilation:

```csharp
// Eager mode: compile during Parse() automatically
var engine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Eager });
var expr = engine.Parse("x + y * 2");  // Compiled immediately
engine.Evaluate(expr);  // Uses compiled delegate (~5-20x faster)

// OnDemand mode (default): explicit compilation
var engine = new CsEvalEngine();
var expr = engine.Parse("x + y * 2");
expr.Compile();  // Compile when you want

// Or use ParseAndCompile for one-step
var expr = engine.ParseAndCompile("x + y * 2");
```

**What compiles:**
- Literals, identifiers, property access
- Arithmetic (`+`, `-`, `*`, `/`, `%`)
- Comparisons (`==`, `!=`, `<`, `>`, `<=`, `>=`)
- Logical (`&&`, `||`, `!`) with short-circuit
- Ternary (`? :`), null coalesce (`??`)

**What falls back to tree-walking:**
- Blocks, loops, switch statements
- Lambdas, LINQ methods
- Assignments, object merging

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

### Async Evaluation

```csharp
var engine = new CsEvalEngine();
var result = await engine.EvaluateAsync("items.Select(x => x.Process())");
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

Arithmetic follows C# promotion rules:
- Small types (`byte`, `short`, etc.) promote to `int`
- `int + int` → `int`
- `int + long` → `long`

### Nullables

Null can be used with any reference type or nullable value type.

### Method Parameters

When calling methods, CsEval:
1. Checks for exact type match
2. Attempts `Convert.ChangeType` for compatible types
3. Uses default values for missing optional parameters
4. Auto-appends `CancellationToken` if method expects it

## Tips

1. **Numeric literals match C#**: `42` is `int`, `42L` is `long`, `3.14` is `double`, `3.14m` is `decimal`. Large integers auto-promote to `long`. Arithmetic follows C# type promotion rules.

2. **Object merging with null**: `null + dict` throws. Use null checks or `??`.

3. **Block scope**: Variables declared with `var` or typed (`int x = 5;`) are scoped to the block. Type keywords (`int`, `long`, etc.) are reserved (matching C# behavior).

4. **Case sensitivity**: Default is case-sensitive. Use `CsEvalOptions { IgnoreCase = true }` for case-insensitive.

5. **Service resolution timing**: `IServiceProvider` is used at evaluation time, not registration time.

6. **Lambda body is single expression**: No blocks in lambda body, use ternary for conditionals.

7. **GroupBy returns dictionaries**: Results have `Key` and `Items` properties, not C#'s `IGrouping<TKey, TElement>`.

8. **Zip without selector**: Returns dictionaries with `First` and `Second` properties, not tuples.

9. **Loop iteration limit**: Loops have a default limit of 100,000 iterations to prevent infinite loops. Configure via `CsEvalOptions.MaxIterations`.

10. **Break/continue scope**: `break` and `continue` only affect the innermost loop.

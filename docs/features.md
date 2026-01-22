# CsEval Features Guide

This document covers the complete feature set of CsEval including LINQ methods, built-in modules, and extensibility options.

## LINQ Methods

All methods work on any `IEnumerable`. Results are `List<object?>` unless otherwise noted.

### Filtering & Projection

```csharp
items.Where(x => x > 0)              // Filter by predicate
items.Select(x => x.Name)            // Project to new shape
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

### Combination

```csharp
items.Concat(otherItems)             // Combine two sequences
```

### Conversion

```csharp
items.ToList()                       // Convert to List<object?>
items.ToArray()                      // Convert to object?[]
```

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

engine.RegisterFunction("double", args => (long)args[0] * 2);
engine.RegisterFunction("greet", args => $"Hello, {args[0]}!");
engine.RegisterFunction("clamp", args => {
    var value = Convert.ToDouble(args[0]);
    var min = Convert.ToDouble(args[1]);
    var max = Convert.ToDouble(args[2]);
    return Math.Clamp(value, min, max);
});

engine.Evaluate("double(5)");        // 10
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

// ~80% faster for repeated evaluation
foreach (var dataset in datasets)
{
    engine.SetVariable("items", dataset);
    var result = engine.Evaluate(expression);
}
```

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

- Integers default to `long`
- Decimals default to `double`
- Automatic conversion when calling methods expecting specific types

```csharp
engine.Evaluate("42");       // long
engine.Evaluate("3.14");     // double
// If method expects int, long is automatically converted
```

### Nullables

Null can be used with any reference type or nullable value type.

### Method Parameters

When calling methods, CsEval:
1. Checks for exact type match
2. Attempts `Convert.ChangeType` for compatible types
3. Uses default values for missing optional parameters
4. Auto-appends `CancellationToken` if method expects it

## Gotchas & Tips

1. **Numbers are `long` by default**: `42` is `long`, not `int`. Use `42.0` for double.

2. **LINQ returns `List<object?>`**: Not `IEnumerable<T>`. Methods like `ToArray()` return `object?[]`.

3. **Object merging with null**: `null + dict` throws. Use null checks or `??`.

4. **Block scope**: Variables declared with `var` are scoped to the block.

5. **Case sensitivity**: Default is case-sensitive. Use `CsEvalOptions { IgnoreCase = true }` for case-insensitive.

6. **Service resolution timing**: `IServiceProvider` is used at evaluation time, not registration time.

7. **Lambda body is single expression**: No blocks in lambda body, use ternary for conditionals.

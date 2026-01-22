# CsEval

A lightweight, high-performance C#-like expression evaluator for .NET. CsEval parses and evaluates expressions at runtime with support for LINQ operations, lambda expressions, control flow, and extensible module registration.

## Features

- **C#-like Syntax**: Familiar syntax with operators, ternaries, null-coalescing, and more
- **LINQ Support**: Full support for Select, Where, OrderBy, Aggregate, and other LINQ methods
- **Lambda Expressions**: `(x) => x * 2`, `(a, b) => a + b`
- **Control Flow**: `if` statements with early returns, block expressions
- **Object Merging**: `+` operator to merge objects and dictionaries
- **Null Safety**: `?.` (null-conditional), `??` (null-coalescing), `??=` (null-coalescing assignment)
- **Interpolated Strings**: `$"Hello, {name}!"`
- **Anonymous Objects**: `new { Name = "John", Age = 30 }`
- **Built-in Modules**: Math, DateTime, Guid, Convert, String, Enumerable
- **Extensible**: Register custom functions, modules, and types
- **High Performance**: Pre-parse expressions for repeated evaluation
- **Case Sensitivity Options**: Configure case-sensitive or case-insensitive evaluation

## Installation

Add CsEval to your project:

```xml
<ProjectReference Include="path/to/CsEval.csproj" />
```

## Quick Start

```csharp
using CsEval;

// Basic evaluation
var engine = new CsEvalEngine();
var result = engine.Evaluate("1 + 2 * 3"); // 7

// With variables
engine.SetVariable("x", 10);
engine.SetVariable("name", "World");
var greeting = engine.Evaluate("$\"Hello, {name}! x = {x}\"");

// LINQ operations
engine.SetVariable("items", new List<object?> { 1, 2, 3, 4, 5 });
var filtered = engine.Evaluate("items.Where(x => x > 2).Select(x => x * 2)");
// Returns [6, 8, 10]

// Anonymous objects
var obj = engine.Evaluate("new { Name = \"John\", Age = 30 }");

// Block expressions with control flow
var result = engine.Evaluate(@"{
    var x = GetValue();
    if (x == null) return null;
    return x + new { Extra = ""computed"" };
}");
```

## Syntax Reference

### Literals

```csharp
42              // integer (long)
3.14            // floating point (double)
"hello"         // string
'hello'         // string (single quotes also supported)
true            // boolean
false           // boolean
null            // null
```

### Operators

```csharp
// Arithmetic
a + b           // addition (also merges objects/dictionaries)
a - b           // subtraction
a * b           // multiplication
a / b           // division
a % b           // modulo
-a              // negation

// Comparison
a == b          // equality
a != b          // inequality
a < b           // less than
a <= b          // less than or equal
a > b           // greater than
a >= b          // greater than or equal

// Logical
a && b          // logical AND
a || b          // logical OR
!a              // logical NOT

// Null handling
a ?? b          // null-coalescing (returns b if a is null)
a?.Property     // null-conditional member access
a ??= b         // null-coalescing assignment (assigns b to a if a is null)

// Ternary
cond ? x : y    // conditional expression
```

### Collections

```csharp
// Array literal
[1, 2, 3]

// Object literal (anonymous object)
new { Name = "John", Age = 30 }

// Index access
arr[0]
dict["key"]
```

### Lambda Expressions

```csharp
x => x * 2                    // single parameter
(x) => x * 2                  // with parentheses
(a, b) => a + b               // multiple parameters
() => 42                      // no parameters
```

### LINQ Methods

All standard LINQ methods are supported on collections:

```csharp
items.Where(x => x > 0)
items.Select(x => x.Name)
items.OrderBy(x => x.Date)
items.OrderByDescending(x => x.Value)
items.First()
items.FirstOrDefault()
items.Last()
items.LastOrDefault()
items.Single()
items.SingleOrDefault()
items.Any()
items.Any(x => x > 0)
items.All(x => x > 0)
items.Count()
items.Count(x => x > 0)
items.Sum()
items.Sum(x => x.Value)
items.Average()
items.Min()
items.Max()
items.Distinct()
items.Take(5)
items.Skip(10)
items.Contains(value)
items.Reverse()
items.Concat(otherItems)
items.ToList()
items.ToArray()
items.Aggregate((acc, x) => acc + x)
items.Aggregate(seed, (acc, x) => acc + x)
```

### Block Expressions

Block expressions allow variable declarations and control flow:

```csharp
{
    var x = 10;
    var y = 20;
    return x + y;
}
```

### If Statements

```csharp
{
    var item = GetItem(id);
    if (item == null) return null;

    var result = ProcessItem(item);
    if (result.HasError) {
        return new { Error = result.Message };
    }

    return result.Value;
}
```

### Object Merging

The `+` operator merges objects and dictionaries:

```csharp
// Merge typed object with anonymous object
person + new { Extra = "data" }

// Result: { Name: "John", Age: 30, Extra: "data" }

// Merge two dictionaries
dict1 + dict2

// Right side properties override left side
base + new { Override = "value" }
```

### Interpolated Strings

```csharp
$"Hello, {name}!"
$"Total: {items.Sum(x => x.Price)}"
$"Date: {DateTime.Now}"
```

## Built-in Modules

### Math

```csharp
Math.Abs(-5)
Math.Floor(3.7)
Math.Ceiling(3.2)
Math.Round(3.5)
Math.Round(3.14159, 2)
Math.Min(a, b)
Math.Max(a, b)
Math.Pow(2, 10)
Math.Sqrt(16)
Math.Sin(x)
Math.Cos(x)
Math.Tan(x)
Math.Log(x)
Math.Log10(x)
Math.Exp(x)
Math.PI
Math.E
```

### DateTime

```csharp
DateTime.Now
DateTime.UtcNow
DateTime.Today
DateTime.MinValue
DateTime.MaxValue
DateTime.Parse("2024-01-15")
```

### Guid

```csharp
Guid.NewGuid()
Guid.Empty
Guid.Parse("...")
```

### Convert

```csharp
Convert.ToInt32(value)
Convert.ToInt64(value)
Convert.ToDouble(value)
Convert.ToBoolean(value)
Convert.ToString(value)
Convert.ToDecimal(value)
```

### String

```csharp
String.Empty
String.IsNullOrEmpty(s)
String.IsNullOrWhiteSpace(s)
String.Join(", ", items)
String.Concat(a, b, c)
String.Format("{0} - {1}", a, b)
```

### Enumerable

```csharp
Enumerable.Range(0, 10)
Enumerable.Repeat("x", 5)
Enumerable.Empty()
```

## Extensibility

### Custom Functions

```csharp
var engine = new CsEvalEngine();
engine.RegisterFunction("double", args => (long)args[0] * 2);
engine.RegisterFunction("greet", args => $"Hello, {args[0]}!");

engine.Evaluate("double(5)");    // 10
engine.Evaluate("greet(\"World\")"); // "Hello, World!"
```

### Custom Modules

```csharp
public class MyModule
{
    public string Process(string input) => input.ToUpper();
    public int Calculate(int a, int b) => a + b;
}

var engine = new CsEvalEngine();
engine.RegisterModule("My", new MyModule());

engine.Evaluate("My.Process(\"hello\")"); // "HELLO"
engine.Evaluate("My.Calculate(5, 3)");    // 8
```

### Attribute-Based Registration

```csharp
[CsEvalModule("Data")]
public class DataModule
{
    public object GetById(string id) => ...;
    public IEnumerable<object> GetAll() => ...;
}

var engine = new CsEvalEngine();
engine.RegisterFromAssembly(typeof(DataModule).Assembly);

engine.Evaluate("Data.GetById(\"123\")");
```

### Global Functions

```csharp
public class GlobalFunctions
{
    [CsEvalFunction("sum")]
    public long Sum(long a, long b) => a + b;

    [CsEvalFunction("format")]
    public string Format(string template, params object[] args)
        => string.Format(template, args);
}

var engine = new CsEvalEngine();
engine.RegisterFromType<GlobalFunctions>();

engine.Evaluate("sum(10, 20)");  // 30
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

## Pre-parsing for Performance

For expressions evaluated multiple times, pre-parse for better performance:

```csharp
var engine = new CsEvalEngine();
var expression = engine.Parse("items.Where(x => x.Active).Sum(x => x.Value)");

// Evaluate multiple times with different data
foreach (var dataset in datasets)
{
    engine.SetVariable("items", dataset);
    var result = engine.Evaluate(expression);
}
```

## Async Evaluation

```csharp
var engine = new CsEvalEngine();
var result = await engine.EvaluateAsync("items.Select(x => x.Process())");
```

## Child Contexts

Create isolated child contexts that inherit parent variables:

```csharp
var parent = new CsEvalEngine();
parent.SetVariable("shared", 100);

var child = parent.CreateChild();
child.SetVariable("local", 50);

child.Evaluate("shared + local"); // 150 - can access both
parent.Evaluate("local");         // Throws - parent can't access child variables
```

## Error Handling

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

// Exception handling
try
{
    var result = engine.Evaluate(expression);
}
catch (ParserException ex)
{
    // Syntax error in expression
}
catch (EvalException ex)
{
    // Runtime evaluation error
}
```

## Cancellation Support

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

## Dependency Injection

CsEval supports resolving module instances from `IServiceProvider`:

```csharp
services.AddScoped<IDataService, DataService>();

// Later...
var engine = new CsEvalEngine();
engine.RegisterModule<IDataService>("Data");

// serviceProvider will be used to resolve IDataService
var result = engine.Evaluate("Data.GetItems()", serviceProvider);
```

## License

MIT

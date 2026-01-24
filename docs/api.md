# CsEval API Reference

## CsEvalEngine

The main entry point for expression evaluation.

### Constructors

```csharp
// Default options (case-sensitive)
var engine = new CsEvalEngine();

// With custom options
var engine = new CsEvalEngine(new CsEvalOptions { IgnoreCase = true });
```

### Parsing

```csharp
// Parse expression (throws on error)
CsEvalExpression Parse(string expression)

// Try parse (returns false on error)
bool TryParse(string expression, out CsEvalExpression? result, out string? error)
bool TryParse(string expression, out CsEvalExpression? result)

// Parse and compile for maximum performance
CsEvalExpression ParseAndCompile(string expression)
```

### Evaluation

```csharp
// Evaluate string expression
object? Evaluate(string expression, IServiceProvider? serviceProvider = null)
object? Evaluate(string expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)

// Evaluate pre-parsed expression
object? Evaluate(CsEvalExpression expression, IServiceProvider? serviceProvider = null)
object? Evaluate(CsEvalExpression expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)

// Generic evaluation with type conversion
T? Evaluate<T>(string expression, IServiceProvider? serviceProvider = null)
T? Evaluate<T>(string expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
T? Evaluate<T>(CsEvalExpression expression, IServiceProvider? serviceProvider = null)
T? Evaluate<T>(CsEvalExpression expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)

// Async evaluation
Task<object?> EvaluateAsync(string expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
Task<object?> EvaluateAsync(CsEvalExpression expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
Task<T?> EvaluateAsync<T>(string expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
Task<T?> EvaluateAsync<T>(CsEvalExpression expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
```

### Variables

```csharp
// Set single variable (fluent)
CsEvalEngine SetVariable(string name, object? value)

// Set multiple variables (fluent)
CsEvalEngine SetVariables(IDictionary<string, object?> variables)
```

### Function Registration

```csharp
// Register custom function (fluent)
CsEvalEngine RegisterFunction(string name, Func<object?[], object?> function)

// Example
engine.RegisterFunction("twice", args => (long)args[0] * 2);
```

### Module Registration

```csharp
// Register module by type (exposes all public methods)
CsEvalEngine RegisterModule(string moduleName, Type type)

// Register module with instance
CsEvalEngine RegisterModule<T>(string moduleName, T? instance = default) where T : class

// Register module with explicit control over exposed methods
CsEvalEngine RegisterModule(string moduleName, Type type, bool explicitOnly)
CsEvalEngine RegisterModule<T>(string moduleName, bool explicitOnly, T? instance = default) where T : class

// Register module with custom member dictionary
CsEvalEngine RegisterModule(string moduleName, Type type, IReadOnlyDictionary<string, MemberInfo> members)

// Register from type (global functions with [CsEvalFunction])
CsEvalEngine RegisterFromType(Type type, object? instance = null)
CsEvalEngine RegisterFromType<T>(T? instance = default) where T : class

// Register all modules/functions from assembly
CsEvalEngine RegisterFromAssembly(Assembly assembly)
```

#### Explicit Module Registration

By default, `RegisterModule` exposes all public methods. Use `explicitOnly: true` to only expose methods marked with `[CsEvalFunction]`:

```csharp
public class MyModule
{
    [CsEvalFunction]           // Exposed as "Calculate"
    public int Calculate(int x) => x * 2;

    [CsEvalFunction("custom")] // Exposed as "custom"
    public int Other(int x) => x + 1;

    public void InternalHelper() { } // Not exposed
}

engine.RegisterModule<MyModule>("Mod", explicitOnly: true);
engine.Evaluate("Mod.Calculate(5)");  // OK - returns 10
engine.Evaluate("Mod.custom(5)");     // OK - returns 6
engine.Evaluate("Mod.InternalHelper()"); // Throws - not exposed
```

Alternatively, set `ExplicitOnly` on the class attribute:

```csharp
[CsEvalModule(ExplicitOnly = true)]
public class SecureModule
{
    [CsEvalFunction]
    public int Safe() => 42;

    public void Dangerous() { } // Not exposed
}

engine.RegisterModule<SecureModule>("Mod"); // Respects ExplicitOnly = true
```

### Context Management

```csharp
// Create child engine with inherited context
CsEvalEngine CreateChild()

// Get registered modules
IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
```

### Properties

```csharp
// Hook for transforming arguments before method invocation
Func<MethodInfo, object?[], object?[]>? ArgumentTransformer { get; set; }
```

## CsEvalOptions

Configuration options for the engine.

```csharp
public sealed class CsEvalOptions
{
    // Default options instance
    public static CsEvalOptions Default { get; }

    // Case-insensitive identifier/property matching
    public bool IgnoreCase { get; init; } = false;

    // Maximum loop iterations (default: 100,000)
    public int MaxIterations { get; init; } = 100_000;

    // Compilation mode (default: OnDemand)
    public CompilationMode CompilationMode { get; init; } = CompilationMode.OnDemand;

    // Sandbox options
    public SandboxOptions Sandbox { get; init; } = new();
}
```

See [sandbox.md](sandbox.md) for sandbox mode documentation.

## CompilationMode

Controls how expressions are evaluated.

```csharp
public enum CompilationMode
{
    // Tree-walk only. No compilation. Best for one-off expressions.
    Disabled,

    // Compile immediately during Parse(). Best for expressions evaluated multiple times.
    // Non-compilable expressions automatically fall back to tree-walking.
    Eager,

    // Only compile when Compile() is called explicitly. Default mode.
    // Gives full control over when compilation happens.
    OnDemand
}
```

Usage:
```csharp
// Eager: best performance for repeated evaluations
var engine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Eager });
var expr = engine.Parse("x + y * 2");  // Compiled automatically

// OnDemand (default): explicit control
var engine = new CsEvalEngine();
var expr = engine.Parse("x + y * 2");
expr.Compile();  // Compile when ready

// Disabled: always interpret (no compilation overhead)
var engine = new CsEvalEngine(new CsEvalOptions { CompilationMode = CompilationMode.Disabled });
```

## CsEvalExpression

Represents a pre-parsed expression.

```csharp
public sealed class CsEvalExpression
{
    // Original expression string
    public string Expression { get; }

    // Returns true if this expression has been successfully compiled
    public bool IsCompiled { get; }

    // Returns true if this expression can be compiled (null if not attempted)
    public bool? IsCompilable { get; }

    // Reason compilation failed (null if succeeded or not attempted)
    public string? CompilationFailureReason { get; }

    // Attempts to compile this expression. Returns true if successful.
    public bool TryCompile();

    // Compiles this expression. Throws if compilation fails.
    public void Compile();
}
```

## Attributes

### CsEvalModuleAttribute

Marks a class as a module that will be registered under a specific name.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class CsEvalModuleAttribute : Attribute
{
    public CsEvalModuleAttribute();
    public CsEvalModuleAttribute(string name);
    public string? Name { get; }
    public bool ExplicitOnly { get; init; }
}
```

Usage:
```csharp
[CsEvalModule("Data")]
public class DataModule
{
    public object GetById(string id) { ... }
}

// With ExplicitOnly - only methods marked with [CsEvalFunction] are exposed
[CsEvalModule(ExplicitOnly = true)]
public class RestrictedModule
{
    [CsEvalFunction]
    public string Allowed() => "ok";

    public string Hidden() => "not accessible";
}
```

### CsEvalFunctionAttribute

Marks a method as exposed to CsEval expressions.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class CsEvalFunctionAttribute : Attribute
{
    public CsEvalFunctionAttribute();
    public CsEvalFunctionAttribute(string name);
    public string? Name { get; }
}
```

Usage:
```csharp
public class GlobalFunctions
{
    [CsEvalFunction("sum")]
    public long Sum(long a, long b) => a + b;
}
```

## Exceptions

### ParserException

Thrown when expression parsing fails.

```csharp
public class ParserException : Exception
{
    public ParserException(string message);
}
```

### LexerException

Thrown when tokenization fails.

```csharp
public class LexerException : Exception
{
    public LexerException(string message);
}
```

### EvalException

Thrown when expression evaluation fails.

```csharp
public class EvalException : Exception
{
    public EvalException(string message);
}
```

## EvalContext

Internal class for managing variable scope. Exposed for advanced scenarios.

```csharp
public sealed class EvalContext
{
    // Constructor
    public EvalContext(StringComparer? comparer = null);

    // Define variable in current scope
    public void Define(string name, object? value);

    // Get variable value (searches parent scopes)
    public object? Get(string name);

    // Set existing variable value (searches parent scopes)
    public void Set(string name, object? value);

    // Try get variable
    public bool TryGet(string name, out object? value);

    // Create child scope
    public EvalContext CreateChild();

    // Get all variables in current scope
    public IReadOnlyDictionary<string, object?> GetAll();

    // Create from ExpandoObject
    public static EvalContext FromExpandoObject(ExpandoObject? expando, StringComparer? comparer = null);

    // Create from dictionary
    public static EvalContext FromDictionary(IDictionary<string, object?>? dict, StringComparer? comparer = null);
}
```

## Built-in Modules

### Math

```csharp
Math.Abs(double value)
Math.Floor(double value)
Math.Ceiling(double value)
Math.Round(double value)
Math.Round(double value, int digits)
Math.Min(double a, double b)
Math.Max(double a, double b)
Math.Pow(double x, double y)
Math.Sqrt(double value)
Math.Sin(double value)
Math.Cos(double value)
Math.Tan(double value)
Math.Log(double value)
Math.Log10(double value)
Math.Exp(double value)
Math.PI  // property
Math.E   // property
```

### DateTime

```csharp
DateTime.Now      // property
DateTime.UtcNow   // property
DateTime.Today    // property
DateTime.MinValue // property
DateTime.MaxValue // property
DateTime.Parse(string s)
DateTime.TryParse(string s, out DateTime result)
```

### Guid

```csharp
Guid.NewGuid()
Guid.Empty  // property
Guid.Parse(string s)
Guid.TryParse(string s, out Guid result)
```

### Convert

```csharp
Convert.ToInt32(object? value)
Convert.ToInt64(object? value)
Convert.ToDouble(object? value)
Convert.ToBoolean(object? value)
Convert.ToString(object? value)
Convert.ToDecimal(object? value)
```

### String

```csharp
String.Empty  // property
String.IsNullOrEmpty(string? value)
String.IsNullOrWhiteSpace(string? value)
String.Join(string separator, IEnumerable<object?> values)
String.Concat(params object?[] values)
String.Format(string format, params object?[] args)
```

### Enumerable

```csharp
Enumerable.Range(int start, int count)
Enumerable.Repeat<T>(T element, int count)
Enumerable.Empty<T>()
```

## LINQ Methods

Available on any `IEnumerable`:

```csharp
.Where(predicate)
.Select(selector)
.SelectMany(selector)
.Aggregate(accumulator)
.Aggregate(seed, accumulator)
.First()
.First(predicate)
.FirstOrDefault()
.FirstOrDefault(predicate)
.Last()
.Last(predicate)
.LastOrDefault()
.LastOrDefault(predicate)
.Single()
.Single(predicate)
.SingleOrDefault()
.SingleOrDefault(predicate)
.Any()
.Any(predicate)
.All(predicate)
.Count()
.Count(predicate)
.Sum()
.Sum(selector)
.Average()
.Average(selector)
.Min()
.Min(selector)
.Max()
.Max(selector)
.OrderBy(keySelector)
.OrderByDescending(keySelector)
.GroupBy(keySelector)
.Distinct()
.Take(count)
.Skip(count)
.Contains(value)
.Reverse()
.Concat(other)
.Zip(other)
.Zip(other, resultSelector)
.ToList()
.ToArray()
```

## Usage Patterns

### Basic Evaluation

```csharp
var engine = new CsEvalEngine();
var result = engine.Evaluate("1 + 2 * 3");
```

### With Variables

```csharp
var engine = new CsEvalEngine()
    .SetVariable("x", 10)
    .SetVariable("name", "World");

var result = engine.Evaluate("x * 2");
var greeting = engine.Evaluate("$\"Hello, {name}!\"");
```

### With Custom Functions

```csharp
var engine = new CsEvalEngine();
engine.RegisterFunction("clamp", args =>
{
    var value = Convert.ToDouble(args[0]);
    var min = Convert.ToDouble(args[1]);
    var max = Convert.ToDouble(args[2]);
    return Math.Clamp(value, min, max);
});

var result = engine.Evaluate("clamp(150, 0, 100)"); // 100
```

### With Custom Modules

```csharp
public class DataService
{
    public object GetUser(string id) { ... }
    public IEnumerable<object> GetAllUsers() { ... }
}

var engine = new CsEvalEngine();
engine.RegisterModule("Users", new DataService());

var user = engine.Evaluate("Users.GetUser(\"123\")");
var names = engine.Evaluate("Users.GetAllUsers().Select(u => u.Name)");
```

### With Dependency Injection

```csharp
[CsEvalModule("Data")]
public class DataModule
{
    private readonly IDbContext _db;
    public DataModule(IDbContext db) => _db = db;
    public object GetById(int id) => _db.Find(id);
}

// In Startup
services.AddScoped<DataModule>();

// In evaluation
var engine = new CsEvalEngine();
engine.RegisterFromAssembly(typeof(DataModule).Assembly);

// serviceProvider resolves DataModule from DI
var result = engine.Evaluate("Data.GetById(123)", serviceProvider);
```

### Pre-parsing for Performance

```csharp
var engine = new CsEvalEngine();
var expr = engine.Parse("items.Where(x => x.Active).Sum(x => x.Value)");

// Evaluate many times
foreach (var dataset in datasets)
{
    engine.SetVariable("items", dataset.Items);
    var total = engine.Evaluate<double>(expr);
    Console.WriteLine($"{dataset.Name}: {total}");
}
```

### Child Contexts

```csharp
var parent = new CsEvalEngine();
parent.SetVariable("baseUrl", "https://api.example.com");
parent.RegisterModule("Http", new HttpModule());

// Each request gets its own context
foreach (var request in requests)
{
    var child = parent.CreateChild();
    child.SetVariable("requestId", request.Id);
    child.SetVariable("userId", request.UserId);

    var result = child.Evaluate(request.Expression);
}
```

### Cancellation

```csharp
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    var result = await engine.EvaluateAsync(
        "items.Select(x => ExpensiveOperation(x))",
        serviceProvider,
        cts.Token
    );
}
catch (OperationCanceledException)
{
    Console.WriteLine("Evaluation timed out");
}
```

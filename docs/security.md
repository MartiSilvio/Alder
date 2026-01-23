# Security

CsEval provides security options to restrict expression evaluation in untrusted contexts.

## SafeMode

SafeMode restricts what expressions can do when evaluating user-provided input.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
});
```

### What SafeMode Blocks

| Operation | Example | Blocked |
|-----------|---------|:-------:|
| Method calls on variables | `user.GetType()`, `list.Add(1)` | Yes |
| Property reads | `user.Name` | No* |
| Index access | `arr[0]`, `dict["key"]` | No |
| Module methods | `Math.Abs(-5)` | No |
| LINQ methods | `items.Where(x => x > 0)` | No |
| Registered functions | `myFunc(x)` | No |

*Property reads can be blocked with `AllowPropertyRead = false`

### Why SafeMode Matters

Without SafeMode, expressions can call arbitrary methods on passed objects:

```csharp
// DANGEROUS: Could access reflection APIs
engine.SetVariable("obj", someObject);
engine.Evaluate("obj.GetType().Assembly"); // Access to Assembly!
```

With SafeMode enabled:

```csharp
engine.Evaluate("obj.GetType()"); // Throws EvalException
engine.Evaluate("obj.Name");       // OK - property read
engine.Evaluate("Math.Abs(-5)");   // OK - registered module
```

## Security Options

```csharp
public sealed class SecurityOptions
{
    // Master switch - blocks method calls on variable objects
    public bool SafeMode { get; init; } = false;

    // When SafeMode=true, also block property reads
    public bool AllowPropertyRead { get; init; } = true;

    // When SafeMode=true, also block variable reassignment
    public bool AllowAssignment { get; init; } = true;
}
```

### Configuration Examples

**Default (no restrictions):**
```csharp
var engine = new CsEvalEngine(); // SafeMode = false
```

**Block method calls only:**
```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Security = new CsEvalOptions.SecurityOptions { SafeMode = true }
});
// user.GetType() - blocked
// user.Name - allowed
// items.Where(...) - allowed (LINQ)
```

**Block everything except modules:**
```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Security = new CsEvalOptions.SecurityOptions
    {
        SafeMode = true,
        AllowPropertyRead = false
    }
});
// user.GetType() - blocked
// user.Name - blocked
// Math.Abs(-5) - allowed (module)
```

**Block variable reassignment (read-only mode):**
```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Security = new CsEvalOptions.SecurityOptions
    {
        SafeMode = true,
        AllowAssignment = false
    }
});
// var x = 5 - allowed (declaration)
// x = 10 - blocked (reassignment)
// x += 5 - blocked (compound assignment)
// x++ - blocked (increment)
// x ??= 5 - blocked if x is null
```

## What's Always Allowed

Even in the strictest SafeMode configuration:

1. **Registered modules** - Methods on explicitly registered modules
2. **Registered functions** - Custom functions via `RegisterFunction()`
3. **Built-in LINQ** - Where, Select, Sum, etc. (handled internally)
4. **Arithmetic and logic** - Operators, literals, variables
5. **Control flow** - if, for, while, switch, etc.

## Design Principles

CsEval's security model follows these principles:

1. **Explicit over implicit** - Only registered modules are accessible by name
2. **Read-only by default** - No property/index SET (not yet implemented)
3. **LINQ is safe** - Handled internally, not via reflection
4. **Fail closed** - SafeMode blocks unknown operations

## Comparison with Competitors

| Feature | CsEval | ExpressionEvaluator | Eval-Expression.NET |
|---------|:------:|:-------------------:|:-------------------:|
| SafeMode | Yes | 15+ granular options | Yes |
| Block method calls | Yes | Yes | Yes |
| Block property reads | Yes | Yes | Yes |
| LINQ always allowed | Yes | N/A | N/A |
| Module whitelist | Yes | Namespace-based | Type-based |

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
| Method calls on variables | `list.Add(1)`, `str.ToLower()` | Yes |
| Reflection methods | `obj.GetType()` | Always* |
| Property reads | `user.Name` | No** |
| Property writes | `user.Name = "new"` | No*** |
| Index reads | `arr[0]`, `dict["key"]` | No |
| Index writes | `arr[0] = value` | No**** |
| Module methods | `Math.Abs(-5)` | No |
| LINQ methods | `items.Where(x => x > 0)` | No |
| Registered functions | `myFunc(x)` | No |

*Reflection is blocked in all modes, see [Reflection Blocking](#reflection-blocking)
**Property reads can be blocked with `AllowPropertyRead = false`
***Property writes can be blocked with `AllowPropertySet = false`
****Index writes can be blocked with `AllowIndexSet = false`

### Why SafeMode Matters

Without SafeMode, expressions can call arbitrary methods on passed objects:

```csharp
engine.SetVariable("list", new List<int> { 1, 2, 3 });
engine.Evaluate("list.Clear()");  // Mutates the list!
engine.Evaluate("list.Add(99)");  // Adds to the list!
```

With SafeMode enabled:

```csharp
engine.Evaluate("list.Clear()");   // Throws EvalException
engine.Evaluate("list.Count");     // OK - property read
engine.Evaluate("Math.Abs(-5)");   // OK - registered module
```

> **Note**: `GetType()` is blocked in all modes due to [Reflection Blocking](#reflection-blocking).

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

    // When SafeMode=true, also block property assignment
    public bool AllowPropertySet { get; init; } = true;

    // When SafeMode=true, also block index assignment
    public bool AllowIndexSet { get; init; } = true;
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

**Block property and index writes (immutable objects):**
```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Security = new CsEvalOptions.SecurityOptions
    {
        SafeMode = true,
        AllowPropertySet = false,
        AllowIndexSet = false
    }
});
// obj.Name - allowed (read)
// obj.Name = "new" - blocked (property set)
// arr[0] - allowed (read)
// arr[0] = 5 - blocked (index set)
```

**Full read-only mode (no mutations):**
```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Security = new CsEvalOptions.SecurityOptions
    {
        SafeMode = true,
        AllowAssignment = false,
        AllowPropertySet = false,
        AllowIndexSet = false
    }
});
// Only variable declarations, reads, and pure expressions allowed
```

## What's Always Allowed

Even in the strictest SafeMode configuration:

1. **Registered modules** - Methods on explicitly registered modules
2. **Registered functions** - Custom functions via `RegisterFunction()`
3. **Built-in LINQ** - Where, Select, Sum, etc. (handled internally)
4. **Arithmetic and logic** - Operators, literals, variables
5. **Control flow** - if, for, while, switch, etc.

## Reflection Blocking

CsEval blocks access to reflection types in **all modes** (not just SafeMode). This is a fundamental security invariant: user code must never obtain a value whose runtime type is `System.Type` or any reflection metadata type.

### Blocked Types

| Type | Description |
|------|-------------|
| `System.Type` | Including `RuntimeType` |
| `System.Reflection.MemberInfo` | Base for MethodInfo, PropertyInfo, FieldInfo, etc. |
| `System.Reflection.Assembly` | Assembly references |
| `System.Reflection.Module` | Module references |
| `RuntimeTypeHandle`, `RuntimeMethodHandle`, `RuntimeFieldHandle` | Runtime handles |

### What Gets Blocked

```csharp
// All of these throw EvalException, regardless of SafeMode setting:
engine.Evaluate("obj.GetType()");           // Returns Type
engine.Evaluate("holder.TypeProperty");      // Property returning Type
engine.Evaluate("items.Select(x => x.GetType())"); // LINQ returning Types
engine.Evaluate("arr[0]");                   // If arr[0] contains a Type
engine.Evaluate("Module.GetMethodInfo()");   // Module returning MethodInfo
```

### Why This Matters

Reflection access enables sandbox escapes:

```csharp
// Without reflection blocking, an attacker could:
obj.GetType().Assembly.GetTypes()  // Enumerate all types
obj.GetType().GetMethod("...").Invoke(...)  // Call arbitrary methods
Type.GetType("System.IO.File").GetMethod("Delete")  // Access file system
```

By blocking all reflection types at the evaluation boundary, these attack vectors are eliminated regardless of other security settings.

### Interaction with SafeMode

- **SafeMode OFF**: Method calls allowed, but reflection types blocked on return
- **SafeMode ON**: Method calls blocked entirely (reflection guard never reached)

Both configurations prevent reflection access, but SafeMode provides additional protection by blocking all method calls.

## Design Principles

CsEval's security model follows these principles:

1. **Explicit over implicit** - Only registered modules are accessible by name
2. **Reflection is forbidden** - No reflection types can escape to user code
3. **Configurable mutation** - Property/index writes can be blocked via security options
4. **LINQ is safe** - Handled internally, not via reflection
5. **Fail closed** - SafeMode blocks unknown operations

## Comparison with Competitors

| Feature | CsEval | ExpressionEvaluator | Eval-Expression.NET |
|---------|:------:|:-------------------:|:-------------------:|
| SafeMode | Yes | 15+ granular options | Yes |
| Block reflection types | Always | Configurable | Configurable |
| Block method calls | Yes | Yes | Yes |
| Block property reads | Yes | Yes | Yes |
| LINQ always allowed | Yes | N/A | N/A |
| Module whitelist | Yes | Namespace-based | Type-based |

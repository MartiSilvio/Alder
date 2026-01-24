# Sandbox

CsEval provides sandbox options to restrict expression evaluation in untrusted contexts.

## Quick Start

```csharp
// Pick a preset - 99% of users stop here
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Safe()
});

// Power users: override specific settings
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Safe() with { AllowAssignment = false }
});
```

## Modes

| Mode | Method Calls | Property Read | Assignment | Property/Index Set |
|------|:------------:|:-------------:|:----------:|:------------------:|
| `Trusted()` | Yes | Yes | Yes | Yes |
| `Safe()` | No | Yes | Yes | Yes |
| `Strict()` | No | Yes | No | No |

### Trusted (Default)

Full access. All operations allowed.

```csharp
var engine = new CsEvalEngine(); // Sandbox = SandboxOptions.Trusted()
engine.SetVariable("list", new List<int> { 1, 2, 3 });
engine.Evaluate("list.Add(4)");  // OK
engine.Evaluate("list.Clear()"); // OK
```

### Safe

Blocks method calls on variable objects. Property reads, assignments, LINQ, and modules still allowed.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Safe()
});
engine.SetVariable("list", new List<int> { 1, 2, 3 });

engine.Evaluate("list.Add(4)");     // Throws EvalException
engine.Evaluate("list.Count");      // OK - property read
engine.Evaluate("list.Sum()");      // OK - LINQ
engine.Evaluate("Math.Abs(-5)");    // OK - module
```

### Strict

Read-only mode. No method calls, no assignments, no property/index writes.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Strict()
});

engine.Evaluate("{ var x = 42; return x; }");       // OK
engine.Evaluate("{ var x = 1; x = 5; return x; }"); // Throws
engine.Evaluate("text.ToUpper()");                  // Throws
engine.Evaluate("obj.Value = 42");                  // Throws
```

## Overrides

Override any setting from a preset using `with`:

```csharp
// Safe mode but block property reads too
Sandbox = SandboxOptions.Safe() with { AllowPropertyRead = false }

// Strict mode but allow assignments
Sandbox = SandboxOptions.Strict() with { AllowAssignment = true }

// Safe mode but allow method calls (back to Trusted behavior)
Sandbox = SandboxOptions.Safe() with { AllowMethodCalls = true }
```

### Available Options

| Option | Description | Trusted | Safe | Strict |
|--------|-------------|:-------:|:----:|:------:|
| `AllowMethodCalls` | `str.ToUpper()` | Yes | No | No |
| `AllowPropertyRead` | `str.Length` | Yes | Yes | Yes |
| `AllowAssignment` | `x = 5`, `x++` | Yes | Yes | No |
| `AllowPropertySet` | `obj.Name = "new"` | Yes | Yes | No |
| `AllowIndexSet` | `arr[0] = 5` | Yes | Yes | No |

## Always Allowed

Even in the strictest configuration:

- **Registered modules** - `Math.Abs()`, custom modules
- **Registered functions** - `RegisterFunction()` callbacks
- **LINQ methods** - `Where`, `Select`, `Sum`, etc.
- **Operators** - Arithmetic, logic, comparisons
- **Control flow** - `if`, `for`, `while`, `switch`
- **Variable declarations** - `var x = 5`

## Reflection Blocking

CsEval blocks reflection types in **all modes**. User code can never obtain:

- `System.Type` (including `RuntimeType`)
- `System.Reflection.MemberInfo` and subclasses
- `System.Reflection.Assembly`
- `System.Reflection.Module`
- Runtime handles

```csharp
// All throw EvalException, regardless of mode:
engine.Evaluate("obj.GetType()");
engine.Evaluate("holder.TypeProperty");
engine.Evaluate("items.Select(x => x.GetType())");
```

This prevents sandbox escapes via reflection:

```csharp
// Without blocking, an attacker could:
obj.GetType().Assembly.GetTypes()
obj.GetType().GetMethod("...").Invoke(...)
Type.GetType("System.IO.File").GetMethod("Delete")
```

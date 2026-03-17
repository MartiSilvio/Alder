---
title: "Sandbox Overview"
description: "Threat model, permission flags, presets, AllowedTypes allowlist, and reflection leak guard."
sidebar:
  order: 1
---

## Threat Model

CsEval evaluates user-supplied C# expressions **in-process**. The sandbox controls which operations those expressions can perform within the .NET runtime.

**What CsEval protects against:**

- Untrusted expressions calling arbitrary methods on host objects
- Untrusted expressions mutating host state (property sets, index writes, variable reassignment)
- Untrusted expressions constructing arbitrary objects
- Untrusted expressions accessing reflection metadata (Type, Assembly, MemberInfo)
- Runaway expressions (infinite loops, deep nesting, long computations)

**What CsEval does NOT protect against:**

- **Process-level isolation** -- CsEval runs in your process. It is not an OS sandbox, container, or AppDomain boundary.
- **Network isolation** -- If a host-injected object has network access, sandbox flags do not prevent it from being read.
- **File system isolation** -- CsEval does not intercept I/O calls at the OS level.
- **Memory isolation** -- Expressions share the host process memory space.
- **Side effects through registered functions** -- If you register a function that deletes files, the sandbox cannot prevent it from being called (registered functions are always allowed).

The sandbox is a **language-level capability filter**, not an OS-level security boundary. Use it to restrict what expressions can do with the objects and types you expose.

## Defense Layers

CsEval provides four independent defense layers:

1. **Sandbox permission flags** -- Eight boolean flags on `SandboxOptions` that control method calls, property access, assignment, construction, and more. See [Permission Flags](#permission-flags) below.
2. **Reflection leak guard** -- Always-on guard that blocks expressions from obtaining reflection metadata (Type, MemberInfo, Assembly). Independent of sandbox flags. See [Reflection Leak Guard](#reflection-leak-guard).
3. **Execution limits** -- Caps on statement count, wall-clock time, and expression nesting depth. See [Execution Limits](../security/execution-limits/).
4. **AllowedTypes allowlist** -- Optional type-level restriction that limits which types can be resolved, constructed, or accessed. See [AllowedTypes Allowlist](#allowedtypes-allowlist).

## Permission Flags

`SandboxOptions` is a sealed C# record with eight boolean flags. All flags default to `false` (deny-all).

| Flag | Controls | Example blocked | Example allowed |
|------|----------|----------------|-----------------|
| `AllowMethodCalls` | Method calls on objects | `str.ToUpper()`, `list.Add(x)` | Modules, registered functions, lambdas, LINQ |
| `AllowPropertyRead` | Property/field reads on objects | `str.Length`, `obj.Name` | |
| `AllowStaticPropertyRead` | Static property reads from types | `DateTime.Now` | |
| `AllowStaticFieldRead` | Static field reads from types | `string.Empty` | |
| `AllowAssignment` | Variable reassignment | `x = 5`, `x++`, `x += 1` | `var x = 5` (declarations always allowed) |
| `AllowPropertySet` | Property/field writes on objects | `obj.Name = "new"` | |
| `AllowIndexSet` | Index writes | `arr[0] = 5`, `dict["key"] = value` | |
| `AllowConstruction` | `new` expressions | `new List<int>()`, `new { }` | |

**AllowMethodCalls details:**

When `AllowMethodCalls` is `false`, instance and static method calls on variable objects and resolved types are blocked. However, the following are **never blocked** regardless of this flag:

- Registered functions (`engine.RegisterFunction(...)`)
- Module methods (`Math.Abs(...)`)
- Lambda/delegate invocations (`myFunc(5)`)
- LINQ extension methods (`items.Where(x => x > 2).Sum()`)

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Safe()
});
engine.SetVariable("name", "alice");

name.ToUpper()
// throws CsEvalSandboxException

name.Length
// output: 5
```

**AllowAssignment details:**

When `AllowAssignment` is `false`, variable reassignment (`x = 5`), compound assignment (`x += 1`), and increment/decrement (`x++`, `--x`) are blocked. Variable declarations (`var x = 5`) are **always allowed** regardless of this flag.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Strict()
});

{ var x = 5; return x; }
// output: 5

{ var x = 1; x = 5; return x; }
// throws CsEvalSandboxException
```

**AllowConstruction details:**

When `AllowConstruction` is `false`, all `new` expressions are blocked -- including `new List<int>()`, `new { Name = "a" }` (anonymous objects), and `new int[] { 1, 2 }`.

## What's Always Allowed

Regardless of sandbox flags, expressions can always:

- **Declare variables** -- `var x = 5`, `int y = 10`
- **Call registered functions** -- Functions registered via `RegisterFunction`
- **Call module methods** -- Methods on registered modules (`Math.Abs(...)`)
- **Create lambdas** -- `(x) => x * 2` (creation; invocation is also always allowed)
- **Invoke delegates** -- Both host-injected delegates and expression-defined lambdas
- **Use LINQ extension methods** -- `items.Where(...)`, `items.Select(...)`, `items.Sum()`
- **Evaluate literal expressions** -- `42`, `"hello"`, `true`, `3.14m`
- **Use typeof()** -- `typeof(int)` returns a type literal (not blocked by reflection guard)
- **Use arithmetic and logical operators** -- `2 + 3`, `x > 5`, `a && b`

:::note
`typeof(int)` is always allowed because it is a compile-time type literal. The reflection leak guard only blocks **runtime** access to Type values through member access or method returns.
:::

## Factory Presets

Three factory methods provide common sandbox configurations:

| Flag | Trusted | Safe | Strict |
|------|---------|------|--------|
| `AllowMethodCalls` | true | false | false |
| `AllowPropertyRead` | true | true | true |
| `AllowStaticPropertyRead` | true | false | false |
| `AllowStaticFieldRead` | true | false | false |
| `AllowAssignment` | true | true | false |
| `AllowPropertySet` | true | true | false |
| `AllowIndexSet` | true | true | false |
| `AllowConstruction` | true | false | false |

**Trusted** -- full access. Use for internal, developer-authored expressions.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Trusted()
});
engine.SetVariable("name", "alice");

name.ToUpper()
// output: "ALICE"
```

**Safe** -- blocks method calls and construction, but allows property access, assignment, and index operations. Use for user expressions that need to read and modify data but should not call arbitrary methods.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Safe()
});
engine.SetVariable("name", "alice");

name.Length
// output: 5

name.ToUpper()
// throws CsEvalSandboxException
```

**Strict** -- read-only. Only property reads and pure expressions. No method calls, no mutations, no construction.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Strict()
});
engine.SetVariable("name", "alice");

name.Length
// output: 5

{ var x = 1; x = 5; return x; }
// throws CsEvalSandboxException
```

**Custom presets** -- use `with` syntax to adjust any preset:

```csharp
// Strict + method calls (read-only data access with method calls)
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Strict() with { AllowMethodCalls = true }
});
engine.SetVariable("name", "alice");

name.ToUpper()
// output: "ALICE"

{ var x = 1; x = 5; return x; }
// throws CsEvalSandboxException (assignment still blocked)
```

## AllowedTypes Allowlist

`SandboxOptions.AllowedTypes` is an optional `HashSet<Type>` that restricts which types can be resolved, constructed, or accessed. When `null` (default), all types in registered assemblies are available.

The allowlist checks the **exact constructed type**. Use `typeof(List<int>)`, not `typeof(List<>)`.

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Trusted() with
    {
        AllowedTypes = new HashSet<Type> { typeof(List<int>), typeof(int) }
    }
});

new List<int> { 1, 2, 3 }
// succeeds -- List<int> is in the allowlist

new List<string> { "a", "b" }
// throws CsEvalSandboxException -- List<string> is not in the allowlist
```

## Reflection Leak Guard

The reflection leak guard is a **separate defense layer** that is **always active**, regardless of sandbox flags. Even in `Trusted()` mode, expressions cannot obtain reflection metadata objects.

**Blocked types:**

- `Type` and all subtypes (RuntimeType)
- `MemberInfo` and all subtypes (MethodInfo, PropertyInfo, FieldInfo, ConstructorInfo, EventInfo)
- `Assembly`
- `Module`
- `RuntimeTypeHandle`, `RuntimeMethodHandle`, `RuntimeFieldHandle`
- `MethodBody`
- All types in `System.Reflection.Emit`
- `IntPtr`, `UIntPtr`, pointers
- Arrays and generics containing any of the above

**How it works:**

The guard inspects the runtime return type of every member access and method call. If the returned value is a forbidden reflection type, a `CsEvalSandboxException` is thrown.

`typeof(int)` is **not** blocked because it is a type literal resolved at parse time -- it never flows through the member access guard. But any expression that would return a `Type` through member access is blocked:

```csharp
typeof(int)
// output: System.Int32 (type literal -- always allowed)

text.GetType()
// throws CsEvalSandboxException (returns RuntimeType -- always blocked)
```

This means even in `Trusted()` mode with all flags enabled, `.GetType()` is blocked:

```csharp
var engine = new CsEvalEngine(new CsEvalOptions
{
    Sandbox = SandboxOptions.Trusted()
});
engine.SetVariable("text", "hello");

text.GetType()
// throws CsEvalSandboxException -- reflection guard is independent of sandbox flags
```

## See Also

- [Execution Limits](../security/execution-limits/) -- MaxStatements, MaxTimeout, MaxExpressionDepth
- [Common Mistakes](../security/common-mistakes/) -- Security anti-patterns with wrong/right code pairs

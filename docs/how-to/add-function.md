---
title: Add a function
description: Register a function and call it from Alder expressions using delegate registration or AlderFunction attributes.
---

# Add a function

Use a function when expressions need a global call site such as `clamp(...)` or `greet(...)`.

## Register a delegate

Register a delegate during engine configuration:

```csharp
using Alder;

var engine = new AlderEngine(o =>
{
    o.Functions.Register("clamp", args =>
    {
        var value = Convert.ToDouble(args[0]);
        var min = Convert.ToDouble(args[1]);
        var max = Convert.ToDouble(args[2]);
        return Math.Min(Math.Max(value, min), max);
    });
});
```

Delegate-registered functions receive evaluated arguments as `object?[]`. Alder does not perform automatic conversion on this surface. Type checking and conversion are the delegate author's responsibility.

Registering the same name again replaces the previous entry.

## Register from attributes

Use `[AlderFunction]` to expose methods from a type as global functions:

```csharp
using Alder;
using Alder.Attributes;

public class GlobalHelpers
{
    [AlderFunction("greet")]
    public string Greet(string name) => $"Hello, {name}!";

    [AlderFunction]
    public int Add(int a, int b = 0) => a + b;
}

var engine = new AlderEngine(o =>
{
    o.Modules.RegisterFromType<GlobalHelpers>();
});
```

Naming rules:

- `[AlderFunction("name")]` uses the supplied name
- `[AlderFunction]` uses the method name
- name matching follows `IsCaseSensitive`

Argument rules:

- expression arguments are mapped to method parameters
- optional parameters use their default values when omitted
- missing required parameters fail at runtime

## Call

```csharp
var a = engine.Evaluate<double>("clamp(150, 0, 100)");
var b = engine.Evaluate<string>("greet(\"Alice\")");
var c = engine.Evaluate<int>("Add(5)");
var d = engine.Evaluate<int>("Add(5, 2)");
```

## Verify

```csharp
if (engine.Evaluate<int>("Add(5, 2)") != 7)
    throw new Exception("Function registration failed.");
```

## Troubleshooting

- Function not found: ensure registration runs before evaluation.
- Wrong name: check `Functions.Register(...)` or `[AlderFunction(...)]`.
- Case mismatch: use exact casing or set `IsCaseSensitive = false`.
- Wrong argument type in a delegate-registered function: convert values explicitly inside the delegate.
- Missing required parameters on an attribute-registered method: supply the argument or add a default.

## Related pages

- [Add a module](/how-to/add-module/)
- [Configuration](/reference/configuration/)

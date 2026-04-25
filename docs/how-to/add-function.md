---
title: Add a function
description: Register a global function for Alder expressions through a delegate or AlderFunction attribute.
---

# Add a function

Use a function when expressions need a global call site owned by the host application: `clamp(...)`, `distance(...)`, `isBusinessDay(...)`, or another operation that should be callable without a module qualifier.

## Register a delegate

Delegate registration is the most direct path. The function receives evaluated arguments as `object?[]`, and the delegate owns validation and conversion.

```csharp
using Alder;

var engine = new AlderEngine(options =>
{
    options.Functions.Register("clamp", args =>
    {
        var value = Convert.ToDouble(args[0]);
        var min = Convert.ToDouble(args[1]);
        var max = Convert.ToDouble(args[2]);
        return Math.Min(Math.Max(value, min), max);
    });
});

var score = engine.Evaluate<double>("clamp(rawScore, 0, 100)", new { rawScore = 127 });
```

Registering the same function name again replaces the previous delegate. Name matching follows the engine's `IsCaseSensitive` setting.

## Register attributed methods

`[AlderFunction]` exposes methods as global functions when the containing type is registered through `Modules.RegisterFromType(...)` and the type is not an `[AlderModule]`.

```csharp
using Alder;
using Alder.Attributes;

public sealed class GlobalHelpers
{
    [AlderFunction("greet")]
    public string Greet(string name) => $"Hello, {name}!";

    [AlderFunction]
    public int Add(int left, int right = 0) => left + right;
}

var engine = new AlderEngine(options =>
{
    options.Modules.RegisterFromType<GlobalHelpers>();
});

var greeting = engine.Evaluate<string>("""greet("Ada")""");
var total = engine.Evaluate<int>("Add(40, 2)");
```

`[AlderFunction("name")]` uses the supplied expression-facing name. `[AlderFunction]` without a name uses the CLR method name. Optional parameters use their default values when omitted; missing required parameters fail during invocation.

## Choose the registration shape

Use delegate registration when the function is naturally dynamic, already validates its own arguments, or should be assembled inline during engine configuration.

Use attributed methods when the function has a stable CLR signature and should participate in Alder's normal method invocation path. That path gives Alder parameter metadata, optional-argument defaults, and normal method dispatch behavior.

## Troubleshooting

- Function not found: ensure registration runs before engine construction finishes and before evaluation begins.
- Wrong name: check `Functions.Register(...)` or `[AlderFunction(...)]`, including case sensitivity.
- Wrong argument type in a delegate function: convert or validate values inside the delegate.
- Missing required parameter on an attributed method: supply the argument or define a default value.

## Related pages

- [Add a module](/how-to/add-module/)
- [Configuration](/reference/configuration/)

---
title: Add a function
description: Register a function and call it from Alder expressions using delegate registration or AlderFunction attributes.
---

# Add a function

## Goal
Register a function so it can be called by name from expressions.

## When to use this
Use this when you need expression code to call application logic that is not built into Alder.

## Register a delegate function
1. Create an `AlderEngine`.
2. Call `Functions.Register` during configuration.
3. Choose the function name used in expressions.
4. Provide a delegate that reads arguments and returns a value.

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

Notes:
- `Functions.Register` overwrites any existing function with the same name.
- Argument conversion is your responsibility inside the delegate.

Argument model:
- Arguments are passed as `object?[]`.
- Values come from evaluated expression arguments.
- No automatic type conversion is applied.
- The delegate must convert values to the required types.

## Register functions from a type
1. Add methods marked with `[AlderFunction]`.
2. Register the type with `Modules.RegisterFromType<T>()`.
3. Call the function name from expressions.

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

Name rules:
- `[AlderFunction("name")]` uses the attribute name.
- `[AlderFunction]` uses the CLR method name.
- Name matching follows engine case-sensitivity (`IsCaseSensitive`).

Argument rules for `[AlderFunction]` methods:
- Alder maps expression arguments to method parameters.
- Missing optional parameters use default values.
- Missing required parameters fail with a runtime error.

## Call the function from an expression
Use the registered name directly in the expression.

```csharp
var a = engine.Evaluate<double>("clamp(150, 0, 100)"); // 100
var b = engine.Evaluate<string>("greet(\"Alice\")");  // Hello, Alice!
var c = engine.Evaluate<int>("Add(5)");                  // 5
var d = engine.Evaluate<int>("Add(5, 2)");               // 7
```

## Verify the result
Evaluate the expression and verify the returned value matches the expected result.

```csharp
if (engine.Evaluate<int>("Add(5, 2)") != 7)
    throw new Exception("Function registration failed.");
```

## Troubleshooting
- Function not found: confirm the registration runs before `Evaluate`.
- Wrong name: check `[AlderFunction("...")]` and expression spelling.
- Case mismatch: set `IsCaseSensitive = false` or call with exact case.
- Wrong argument type: convert values inside delegate functions.
- Delegate argument gotcha: delegate functions receive raw objects; incorrect casts or missing conversions fail at runtime.
- Missing arguments on `[AlderFunction]` methods: provide required parameters or add method defaults.

## Related pages
- [Configuration](/reference/configuration/)
- [Execution model](/reference/execution-model/)

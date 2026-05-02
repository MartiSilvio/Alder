---
title: Expose functions and modules
description: Expose host-owned APIs to Alder expressions through global functions, named modules, and attribute-based registration.
---

# Expose functions and modules

Use functions and modules when expressions need to call host-owned APIs. A function creates a global call site such as `clamp(...)` or `isBusinessDay(...)`. A module creates a named surface such as `math.CircleArea(...)`, `calendar.IsHoliday(...)`, or `Users.CountActive()`. Exact option and precedence rules are covered in [Configuration](/reference/configuration/).

Registration is a trust decision. Alder can validate whether an expression is allowed to call a registered surface, but the registered function or module still runs as host CLR code. Keep expression-facing APIs narrow, predictable, and side-effect-aware.

## Register a global function

Delegate registration is the most direct path. The function receives evaluated arguments as `object?[]`, and the delegate owns validation and conversion.

<!-- test: Functions_Register -->
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

## Register attributed functions

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

## Register a named module

`Modules.Register<T>(...)` exposes a type under a module name:

<!-- test: Modules_Register -->
```csharp
using Alder;

public sealed class MathTools
{
    public double CircleArea(double radius) => Math.PI * radius * radius;
    public static double Tau => Math.PI * 2;
}

var engine = new AlderEngine(options =>
{
    options.Modules.Register<MathTools>("math");
});

var area = engine.Evaluate<double>("math.CircleArea(5)");
var tau = engine.Evaluate<double>("math.Tau");
```

Static members do not require an instance. Instance members require an instance resolution path.

## Supply module instances

For instance members, Alder resolves the module target in this order:

1. the instance supplied at registration time
2. `AlderOptions.ServiceProvider`
3. a public parameterless constructor
4. failure

Use an explicit instance when the module carries request-specific state:

```csharp
public sealed class TenantRules(string tenantId)
{
    public bool CanDiscount(decimal total) => tenantId == "enterprise" && total >= 500m;
}

var rules = new TenantRules("enterprise");

var engine = new AlderEngine(options =>
{
    options.Modules.Register("rules", typeof(TenantRules), instance: rules);
});

var allowed = engine.Evaluate<bool>("rules.CanDiscount(750m)");
```

Use `ServiceProvider` when module instances should come from the host application's dependency injection container.

Explicit instances are reused for every module access through that engine. Service-provider resolution happens when Alder needs an instance target, so lifetime and scoping follow the host container. The parameterless-constructor path creates module instances on demand and should be reserved for stateless modules.

## Register modules with attributes

`[AlderModule]` lets the type declare its expression-facing name:

```csharp
using Alder;
using Alder.Attributes;

[AlderModule("Text")]
public sealed class TextModule
{
    public string TitleCase(string value) =>
        string.Join(" ", value.Split(' ').Select(word => char.ToUpper(word[0]) + word[1..]));
}

var engine = new AlderEngine(options =>
{
    options.Modules.RegisterFromType<TextModule>();
});

var title = engine.Evaluate<string>("""Text.TitleCase("quarterly report")""");
```

If `RegisterFromType(...)` is used on a type without `[AlderModule]`, methods marked with `[AlderFunction]` are registered as global functions.

## Restrict the exposed module surface

Explicit mode exposes only methods marked with `[AlderFunction]`. It does not expose properties or fields.

<!-- test: Modules_ExplicitOnly -->
```csharp
using Alder.Attributes;

public sealed class AccountModule
{
    [AlderFunction]
    public bool IsActive(int accountId) => accountId > 0;

    public string InternalToken => "hidden";
}

var engine = new AlderEngine(options =>
{
    options.Modules.Register<AccountModule>("accounts", explicitOnly: true);
});

var active = engine.Evaluate<bool>("accounts.IsActive(42)");
```

The same rule is available through `[AlderModule(ExplicitOnly = true)]`. `[AlderFunction("Alias")]` exposes an attributed method under the supplied alias.

## Choose the exposure shape

Use delegate functions when the operation is naturally dynamic, already validates its own arguments, or should be assembled inline during engine configuration.

Use attributed functions when the operation has a stable CLR signature and should participate in Alder's normal method invocation path. That path gives Alder parameter metadata, optional-argument defaults, and normal method dispatch behavior.

Use modules when the API should remain grouped under an expression-facing owner. Modules preserve product boundaries: the host decides which object or type becomes visible, and expressions access that surface through a module name.

For untrusted or tenant-authored expressions, prefer small purpose-built functions and explicit-only modules over broad service objects. That keeps the expression-facing API aligned with the sandbox policy and leaves fewer host capabilities reachable from expressions.

## Expose async APIs

Task-returning functions and module methods are ordinary host call targets. Use `EvaluateAsync(...)` when the expression should await them:

```csharp
public sealed class PricingService
{
    public Task<decimal> GetMinimumAsync(string category) =>
        Task.FromResult(category == "Specialty" ? 250m : 50m);
}

var engine = new AlderEngine(options =>
{
    options.Modules.Register("pricing", typeof(PricingService), instance: new PricingService());
});

var accepted = await engine.EvaluateAsync<bool>(
    """total >= await pricing.GetMinimumAsync(category)""",
    new { total = 300m, category = "Specialty" });
```

`EvaluateAsync(...)` preserves C# await semantics. Awaiting a task produces its result. Returning a task without `await` returns the task object.

## Troubleshooting

- Function not found: ensure registration runs before engine construction finishes and before evaluation begins.
- Module not found: ensure the module is registered under the name used by the expression.
- Member not found: confirm the member is public and exposed under the active explicit-mode rules.
- Wrong argument type in a delegate function: convert or validate values inside the delegate.
- Missing required parameter on an attributed method: supply the argument or define a default value.
- Instance resolution failure: supply an instance, configure `ServiceProvider`, or add a public parameterless constructor.
- Case mismatch: use exact casing or set `IsCaseSensitive = false`.

## Related pages

- [Register types and extension methods](/guides/type-registration/)
- [Configuration](/reference/configuration/)
- [Security model](/operations/security-model/)

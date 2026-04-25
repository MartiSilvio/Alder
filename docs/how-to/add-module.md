---
title: Add a module
description: Register a named module surface whose members can be called from Alder expressions.
---

# Add a module

Use a module when expressions need a named host-owned surface such as `math.CircleArea(...)`, `calendar.IsHoliday(...)`, or `Users.CountActive()`. Modules preserve product boundaries: the host decides which object or type becomes visible, and expressions access that surface through a module name.

## Register a named module

`Modules.Register<T>(...)` exposes a type under a name:

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

If `RegisterFromType(...)` is used on a type without `[AlderModule]`, methods marked with `[AlderFunction]` are registered as global functions instead of module members.

## Restrict the exposed surface

Explicit mode exposes only methods marked with `[AlderFunction]`. It does not expose properties or fields.

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

## Troubleshooting

- Module not found: ensure the module is registered under the name used by the expression.
- Member not found: confirm the member is public and exposed under the active explicit-mode rules.
- Instance resolution failure: supply an instance, configure `ServiceProvider`, or add a public parameterless constructor.
- Case mismatch: use exact casing or set `IsCaseSensitive = false`.

## Related pages

- [Add a function](/how-to/add-function/)
- [Configuration](/reference/configuration/)

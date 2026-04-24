---
title: Add a module
description: Register a module and expose its members to Alder expressions.
---

# Add a module

Use a module when expressions need a named surface such as `utils.CircleArea(...)` or `Users.CountActiveUsers()`.

## Register

Register a module type with `Modules.Register`:

```csharp
using Alder;

public class MathUtils
{
    public double CircleArea(double radius) => Math.PI * radius * radius;
    public static double Tau => Math.PI * 2;
}

var engine = new AlderEngine(o =>
{
    o.Modules.Register<MathUtils>("utils");
});
```

Call the module by name:

```csharp
var area = engine.Evaluate<double>("utils.CircleArea(5)");
var tau = engine.Evaluate<double>("utils.Tau");
```

## Resolve instances from DI

If the module requires application services, configure `IServiceProvider` and register the module type:

```csharp
using Alder;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

public class AppDbContext
{
    public DbSet<User> Users => Set<User>();
}

public class User
{
    public bool IsActive { get; set; }
}

public class UserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public int CountActiveUsers() => _db.Users.AsNoTracking().Count(u => u.IsActive);
}

IServiceProvider appServices = /* your app DI provider */;

var engine = new AlderEngine(o =>
{
    o.ServiceProvider = appServices;
    o.Modules.Register("Users", typeof(UserRepository));
});

var active = engine.Evaluate<int>("Users.CountActiveUsers()");
```

Instance resolution order:

1. explicit instance supplied at registration
2. `IServiceProvider`
3. public parameterless constructor
4. failure

Static members do not require instance resolution.

## Register from attributes

Use `[AlderModule]` when the type should declare its expression-facing name:

```csharp
using Alder;
using Alder.Attributes;

[AlderModule("CustomMath")]
public class CustomMathModule
{
    public long Square(long value) => value * value;
    public static long Cube(long value) => value * value * value;
}

var engine = new AlderEngine(o =>
{
    o.Modules.RegisterFromType<CustomMathModule>();
});
```

Then call it directly:

```csharp
var square = engine.Evaluate<long>("CustomMath.Square(4)");
var cube = engine.Evaluate<long>("CustomMath.Cube(3)");
```

If `RegisterFromType` is used on a type without `[AlderModule]`, methods marked with `[AlderFunction]` are registered as global functions instead.

## Restrict exposure

Use explicit mode when only selected methods should be callable from expressions.

```csharp
public class SecureModule
{
    [AlderFunction]
    public string Allowed() => "ok";

    public string Hidden() => "no";
}

var engine = new AlderEngine(o =>
{
    o.Modules.Register<SecureModule>("secure", explicitOnly: true);
});
```

The same behavior is available through `[AlderModule(ExplicitOnly = true)]`.

In explicit mode:

- only methods marked with `[AlderFunction]` are exposed
- properties and fields are not exposed
- `[AlderFunction("Alias")]` exposes a method under the alias

## Verify

```csharp
if (engine.Evaluate<long>("CustomMath.Square(4)") != 16)
    throw new Exception("Module registration failed.");
```

## Troubleshooting

- Module not found: ensure registration runs before evaluation.
- Wrong module name: check the supplied registration name or `[AlderModule(...)]` value.
- Member not found: confirm the member is public and exposed under the active explicit-mode rules.
- Instance resolution failure: register an instance, configure `IServiceProvider`, or add a public parameterless constructor.
- Case mismatch: use exact casing or set `IsCaseSensitive = false`.

## Related pages

- [Add a function](/how-to/add-function/)
- [Configuration](/reference/configuration/)

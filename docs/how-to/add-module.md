---
title: Add a module
description: Register a module and access its members from Alder expressions.
---

# Add a module

## Goal
Register a module so expressions can access its members using `ModuleName.Member` syntax.

## When to use this
Use this when you need to group related functions and state under a named module.

## Register a module
1. Create a module type with public members.
2. Create an `AlderEngine`.
3. Register the module with `Modules.Register`.
4. Use the module name in expressions.

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

Name rule:
- `Modules.Register("utils", ...)` sets the expression name explicitly.

Instance vs static behavior:
- Static members execute without creating a module instance.
- Instance members execute on a resolved module instance.
- If you pass `instance: someObject`, Alder uses that instance.
- Module instances are resolved per access and are not cached by default.

### Use `IServiceProvider` for module instances
Use this when module instances require dependencies.

1. Configure your application `IServiceProvider`.
2. Assign it to `AlderOptions.ServiceProvider`.
3. Register the module type.
4. Call module members normally.

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
// appServices must resolve UserRepository and AppDbContext.

var engine = new AlderEngine(o =>
{
    o.ServiceProvider = appServices;
    o.Modules.Register("Users", typeof(UserRepository));
});

var active = engine.Evaluate<int>("Users.CountActiveUsers()");
```

## Register a module from a type
1. Add `[AlderModule("Name")]` to the type.
2. Register it with `Modules.RegisterFromType<T>()`.
3. Use the attribute name in expressions.

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

Name resolution rules:
- `Register("name", ...)` uses the provided name.
- `RegisterFromType<T>()` uses `[AlderModule("name")]` when present.
- If `RegisterFromType` is used on a type without `[AlderModule]`, `[AlderFunction]` methods are registered as global functions, not as a module.

## Access module members from expressions
Call module members with `ModuleName.Member`.

```csharp
var area = engine.Evaluate<double>("utils.CircleArea(5)");
var tau = engine.Evaluate<double>("utils.Tau");

var square = engine.Evaluate<long>("CustomMath.Square(4)");
var cube = engine.Evaluate<long>("CustomMath.Cube(3)");
```

## Control exposed members
Use explicit mode when only selected methods must be callable.

Option 1: pass `explicitOnly: true` in registration.

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

Option 2: set `[AlderModule(ExplicitOnly = true)]` on the module type.

Rules in explicit mode:
- Only methods marked with `[AlderFunction]` are exposed.
- Properties and fields are not exposed.
- `[AlderFunction("Alias")]` exposes the method under the alias.

## Verify the result
Evaluate module expressions and check returned values.

```csharp
if (engine.Evaluate<long>("CustomMath.Square(4)") != 16)
    throw new Exception("Module registration failed.");
```

## Troubleshooting
- Module not found: confirm the registration runs before `Evaluate`.
- Wrong module name: check `Register("name", ...)` or `[AlderModule("name")]`.
- Member not found: confirm the member is public and exposed by explicit-mode rules.
- Instance module gotcha: module types must be constructible (public parameterless constructor) or resolvable through `IServiceProvider`; otherwise evaluation fails at runtime.
- Case mismatch: set `IsCaseSensitive = false` or use exact casing in expressions.

## Related pages
- [Configuration](/reference/configuration/)
- [Add a function](/how-to/add-function/)
- [Execution model](/reference/execution-model/)

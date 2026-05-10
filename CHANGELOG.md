# Changelog

## 1.0.2 - 2026-05-09

### Breaking changes

- Removed the predefined `SecurityOptions.Safe()` and `SecurityOptions.Strict()` policies, along with the matching `SecurityPolicy.Safe` and `SecurityPolicy.Strict` cached policies. Alder now exposes only `SecurityOptions.Trusted()` as a named preset; custom policies should use explicit `new SecurityOptions { ... }` initializers so hosts own their security posture.

Migration example:

```csharp
// Before
options.Security = SecurityOptions.Safe();

// After
options.Security = new SecurityOptions
{
    AllowPropertyRead = true,
    AllowStaticPropertyRead = true,
    AllowStaticFieldRead = true,
    AllowAssignment = true,
    AllowPropertySet = true,
    AllowIndexSet = true
};
```

## 1.0.1 - 2026-05-06

Packaging polish: Source Link integration, dedicated NuGet readme, and cleaned package tags.

## 1.0.0 - 2026-05-06

Initial release of Alder: a C# expression runtime with parser, binder, interpreter, opt-in compiled backend, async execution, Dynamic LINQ over `IEnumerable<T>`, `IQueryable<T>`, and `IAsyncEnumerable<T>`, LINQ expression-tree export, host-controlled security policy, execution constraints, and NativeAOT generated dispatch.

# Changelog

## 1.0.5 - 2026-06-19

Documentation fix; no runtime changes. The NuGet package README now uses Markdown instead of raw HTML so the package description renders correctly on NuGet.org.

## 1.0.4 - 2026-06-19

### Added

- `using` resource declarations (`using var resource = ...;` and `using (T resource = ...) { ... }`) and read-only `using`/`foreach` iteration variables, following ECMA-334 §13.9/§13.14.
- `ALDR0319` diagnostic for a missing rooted generic closure in authoritative generated mode.
- Three-way (Roslyn / interpreter / NativeAOT) value-parity harness with `scripts/parity-matrix.sh`, plus `scripts/aot-publish-check.sh` for a strict NativeAOT trim/AOT-warning gate now enforced in CI.
- CI publishes packages to NuGet.org and GitHub Packages on `v*.*.*` tags.

### Changed

- Engine/host AOT faults (`ALDR0316`–`ALDR0319`) now propagate to the host and are no longer catchable by a script's own `try`/`catch`. The interpreter and compiled backend enforce this identically.
- `foreach` over an explicitly-convertible element type now performs a runtime cast (C# CS0030 semantics) instead of failing to bind.
- The default denied-type set is built from direct type references, making it deterministic and AOT-safe regardless of which assemblies happen to be loaded. The host-controlled security surface is unchanged.
- Generated dispatch excludes trim/AOT-unsafe members, and blanket trim pragmas were replaced with narrowly scoped, justified `[UnconditionalSuppressMessage]` annotations.
- `break`/`continue` that would leave a `finally` block are now rejected (CS0157).

### Fixed

- `await` of a `Task<T>` under NativeAOT silently returned `null` because the result was read through a reflection path unavailable under AOT. Await now unwraps through generated dispatch, with per-type accessor caches restored for the JIT path.
- The compiled backend could swallow engine-fault exceptions that the interpreter propagates, a backend-parity divergence; both backends now share one classification.

## 1.0.3 - 2026-05-15

Documentation and packaging refresh; no runtime changes. Tightened the README prose, synced NUGET.md, added a NuGet badge, and updated the async examples to use module registration (`options.Modules.Register<...>`).

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

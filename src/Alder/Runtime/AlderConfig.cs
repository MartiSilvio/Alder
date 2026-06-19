using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Alder.Aot;
using Alder.Compilation;
using Alder.Runtime.Collections;
using Alder.Security;

namespace Alder.Runtime;

/// <summary>
/// Immutable configuration for Alder evaluation contexts.
/// Once created, this configuration is thread-safe and can be shared across multiple threads.
/// Uses FixedDictionary for optimal read performance on immutable data.
/// </summary>
internal sealed class AlderConfig
{
    public LanguageMode LanguageMode { get; }
    public SecurityPolicy Security { get; }
    public bool IsCaseSensitive { get; }
    public ExecutionConstraints Constraints { get; }
    public ICompiledProvider? Compiler { get; }
    public IExpressionCompiler ExpressionCompiler { get; }
    public IServiceProvider? ServiceProvider { get; }
    public FixedDictionary<string, Func<object?[], object?>> Functions { get; }
    internal FixedDictionary<string, ModuleInfo> Modules { get; }
    internal ImmutableArray<Type> ExtensionTypes { get; }
    internal TypeMetadataProvider TypeMetadata { get; }
    internal TypeResolver TypeResolver { get; }
    public StringComparer Comparer { get; }
    public StringComparison StringComparison => IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
    internal FixedDictionary<Type, TypedDispatch>? TypeDispatch { get; }
    internal FixedDictionary<Type, IReadOnlyList<GenericStaticDispatch>>? GenericStaticDispatch { get; }
    internal IReadOnlyDictionary<Type, Func<object, Delegate>>? DelegateFactories { get; }
    internal IReadOnlyCollection<RootedType>? RootedDelegateTypes { get; }

    internal bool TryGetDispatch(Type type, [NotNullWhen(true)] out TypedDispatch? metadata)
    {
        if (TypeDispatch is { } dispatch && dispatch.TryGetValue(type, out metadata))
            return true;

        metadata = null;
        return false;
    }

    internal AlderConfig(
        LanguageMode languageMode,
        SecurityPolicy security,
        bool isCaseSensitive,
        ExecutionConstraints constraints,
        ICompiledProvider? compiler,
        IExpressionCompiler expressionCompiler,
        IServiceProvider? serviceProvider,
        FixedDictionary<string, Func<object?[], object?>> functions,
        FixedDictionary<string, ModuleInfo> modules,
        ImmutableArray<Type> extensionTypes,
        TypeMetadataProvider typeMetadata,
        TypeResolver typeResolver,
        FixedDictionary<Type, TypedDispatch>? typeDispatch,
        FixedDictionary<Type, IReadOnlyList<GenericStaticDispatch>>? genericStaticDispatch = null,
        IReadOnlyDictionary<Type, Func<object, Delegate>>? delegateFactories = null,
        IReadOnlyCollection<RootedType>? rootedDelegateTypes = null)
    {
        LanguageMode = languageMode;
        Security = security;
        IsCaseSensitive = isCaseSensitive;
        Constraints = constraints;
        Compiler = compiler;
        ExpressionCompiler = expressionCompiler;
        ServiceProvider = serviceProvider;
        Functions = functions;
        Modules = modules;
        ExtensionTypes = extensionTypes;
        TypeMetadata = typeMetadata;
        TypeResolver = typeResolver;
        Comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        TypeDispatch = typeDispatch;
        GenericStaticDispatch = genericStaticDispatch;
        DelegateFactories = delegateFactories;
        RootedDelegateTypes = rootedDelegateTypes;
    }

    internal static readonly AlderConfig Empty = new(
        LanguageMode.Standard,
        SecurityOptions.Trusted().ToSecurityPolicy(),
        true,
        new ExecutionConstraints(),
        null,
        DefaultExpressionCompiler.Instance,
        null,
        FixedDictionary<string, Func<object?[], object?>>.Empty,
        FixedDictionary<string, ModuleInfo>.Empty,
        [],
        new TypeMetadataProvider(),
        TypeResolver.Create([], [], true, StringComparer.Ordinal),
        null);
}

internal sealed class ModuleInfo
{
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.PublicProperties |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.Interfaces)]
    public Type Type { get; }

    public object? Instance { get; }
    public IReadOnlyDictionary<string, ModuleMemberEntry> Members { get; }

    public ModuleInfo(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.Interfaces)]
        Type type,
        object? instance,
        IReadOnlyDictionary<string, ModuleMemberEntry> members)
    {
        Type = type;
        Instance = instance;
        Members = members;
    }

    public object Resolve(IServiceProvider? serviceProvider)
    {
        if (Instance != null)
            return Instance;

        var resolved = serviceProvider?.GetService(Type);
        if (resolved != null)
            return resolved;

        if (Type.GetConstructor(Type.EmptyTypes) != null)
        {
            return Activator.CreateInstance(Type)
                   ?? throw new AlderException(Diagnostics.DiagnosticDescriptors.CannotResolveModuleInstance,
                       Type.FullName ?? Type.Name);
        }

        throw new AlderException(Diagnostics.DiagnosticDescriptors.CannotResolveModuleInstance,
            Type.FullName ?? Type.Name);
    }
}

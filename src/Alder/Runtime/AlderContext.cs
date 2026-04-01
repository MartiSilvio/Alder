using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Dynamic;
using System.Runtime.CompilerServices;
using Alder.Diagnostics;
using Alder.Runtime.Collections;

namespace Alder.Runtime;

/// <summary>
/// Thread-safe evaluation context for Alder expressions.
/// Uses ConcurrentDictionary for thread-safe variable access across parent/child relationships.
/// Parent contexts are never modified by child evaluations - children have isolated state.
/// </summary>
internal sealed class AlderContext
{
    private readonly ConcurrentDictionary<string, object?>? _concurrentVariables;
    private readonly ConcurrentDictionary<string, Type>? _concurrentVariableTypes;
    private readonly ConcurrentDictionary<string, bool>? _concurrentReadOnlyVariables;
    private Dictionary<string, object?>? _localVariables;
    private Dictionary<string, Type>? _localVariableTypes;
    private Dictionary<string, bool>? _localReadOnlyVariables;
    private readonly AlderContext? _parent;
    private readonly AlderConfig _config;
    private readonly bool _usesLocalStore;
    private int _variableTypeVersion;

    private static readonly IReadOnlyDictionary<string, object?> EmptyVariables =
        new Dictionary<string, object?>();

    public AlderContext(AlderConfig config) : this(config, null, null, useConcurrentStore: true)
    {
    }

    public AlderContext(AlderConfig config, IServiceProvider? serviceProvider) : this(config, null, serviceProvider, useConcurrentStore: true)
    {
    }

    private AlderContext(AlderConfig config, AlderContext? parent, IServiceProvider? serviceProvider, bool useConcurrentStore)
    {
        _config = config;
        _parent = parent;
        ServiceProvider = serviceProvider ?? parent?.ServiceProvider;
        if (useConcurrentStore)
        {
            _concurrentVariables = new ConcurrentDictionary<string, object?>(_config.Comparer);
            _concurrentVariableTypes = new ConcurrentDictionary<string, Type>(_config.Comparer);
            _concurrentReadOnlyVariables = new ConcurrentDictionary<string, bool>(_config.Comparer);
        }
        else
        {
            _usesLocalStore = true;
        }
    }

    public AlderConfig Config => _config;
    public StringComparer Comparer => _config.Comparer;
    public IServiceProvider? ServiceProvider { get; }
    internal TypeMetadataProvider TypeMetadata => _config.TypeMetadata;
    internal TypeResolver TypeResolver => _config.TypeResolver;
    internal FixedDictionary<string, Func<object?[], object?>> Functions => _config.Functions;
    internal FixedDictionary<string, ModuleInfo> Modules => _config.Modules;
    internal ImmutableArray<Type> ExtensionTypes => _config.ExtensionTypes;

    public void Define(string name, object? value) => SetLocalVariable(name, value);

    public void Define(string name, object? value, Type inferredType, bool isReadOnly = false)
    {
        var typeChanged = !TryGetLocalVariableType(name, out var existingType) || existingType != inferredType;
        SetLocalVariable(name, value);
        SetLocalVariableType(name, inferredType);
        SetLocalReadOnly(name, isReadOnly);
        if (typeChanged)
            Interlocked.Increment(ref _variableTypeVersion);
    }

    /// <summary>
    /// Defines a new variable, enforcing C# shadowing rules.
    /// Throws if the variable already exists in the current scope or any parent scope.
    /// </summary>
    public void DefineNew(string name, object? value, Type inferredType, bool isReadOnly = false)
    {
        if (Contains(name))
            throw new AlderException(DiagnosticDescriptors.DuplicateLocalVariable, name);

        SetLocalVariable(name, value);
        SetLocalVariableType(name, inferredType);
        SetLocalReadOnly(name, isReadOnly);
        Interlocked.Increment(ref _variableTypeVersion);
    }

    public bool TryGetVariableType(string name, out Type? type)
    {
        if (TryGetLocalVariableType(name, out var localType))
        {
            type = localType;
            return true;
        }

        if (_parent != null)
            return _parent.TryGetVariableType(name, out type);

        type = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGet(string name, out object? value)
    {
        if (TryGetLocalVariable(name, out value))
            return true;

        if (_parent != null)
            return _parent.TryGet(name, out value);

        value = null;
        return false;
    }

    public object? Get(string name)
    {
        if (TryGet(name, out var value))
            return value;
        throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetVariableTyped<T>(string name)
    {
        object? value;
        if (_parent == null)
        {
            if (!TryGetLocalVariable(name, out value))
                throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
        }
        else if (!TryGet(name, out value))
        {
            throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
        }

        return TypeHelpers.CoerceToType<T>(value);
    }

    public void Set(string name, object? value)
    {
        if (ContainsLocal(name))
        {
            if (IsLocalReadOnly(name))
                throw new AlderException(DiagnosticDescriptors.ReadonlyAssignment);
            SetLocalVariable(name, value);
            return;
        }

        if (_parent != null && _parent.Contains(name))
        {
            _parent.Set(name, value);
            return;
        }

        throw new AlderException(DiagnosticDescriptors.NameNotInContext, name);
    }

    private bool Contains(string name)
    {
        if (ContainsLocal(name))
            return true;
        return _parent?.Contains(name) ?? false;
    }

    public AlderContext CreateChild()
    {
        return new AlderContext(_config, this, null, useConcurrentStore: false);
    }

    internal void ClearScope()
    {
        if (_usesLocalStore)
        {
            _localVariables?.Clear();
            _localVariableTypes?.Clear();
            _localReadOnlyVariables?.Clear();
        }
        else
        {
            _concurrentVariables!.Clear();
            _concurrentVariableTypes!.Clear();
            _concurrentReadOnlyVariables!.Clear();
        }

        Interlocked.Increment(ref _variableTypeVersion);
    }

    public IReadOnlyDictionary<string, object?> GetAll() =>
        _localVariables ?? (IReadOnlyDictionary<string, object?>?)_concurrentVariables ?? EmptyVariables;

    internal int GetTypeInferenceVersion()
    {
        var version = _variableTypeVersion;
        if (_parent != null)
            version = unchecked((version * 397) ^ _parent.GetTypeInferenceVersion());
        return version;
    }

    public static AlderContext FromExpandoObject(ExpandoObject? expando, AlderConfig config)
    {
        var ctx = new AlderContext(config);
        if (expando == null) return ctx;

        foreach (var kvp in (IDictionary<string, object?>)expando)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }

    public static AlderContext FromDictionary(IDictionary<string, object?>? dict, AlderConfig config)
    {
        var ctx = new AlderContext(config);
        if (dict == null) return ctx;

        foreach (var kvp in dict)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetLocalVariable(string name, out object? value)
    {
        if (_usesLocalStore)
        {
            if (_localVariables != null)
                return _localVariables.TryGetValue(name, out value);
            value = null;
            return false;
        }

        return _concurrentVariables!.TryGetValue(name, out value);
    }

    private bool TryGetLocalVariableType(string name, out Type type)
    {
        if (_usesLocalStore)
        {
            if (_localVariableTypes != null)
                return _localVariableTypes.TryGetValue(name, out type!);
            type = null!;
            return false;
        }

        return _concurrentVariableTypes!.TryGetValue(name, out type!);
    }

    private bool ContainsLocal(string name)
    {
        if (_usesLocalStore)
            return _localVariables != null && _localVariables.ContainsKey(name);

        return _concurrentVariables!.ContainsKey(name);
    }

    private void SetLocalVariable(string name, object? value)
    {
        if (_usesLocalStore)
            (_localVariables ??= new Dictionary<string, object?>(_config.Comparer))[name] = value;
        else
            _concurrentVariables![name] = value;
    }

    private void SetLocalVariableType(string name, Type type)
    {
        if (_usesLocalStore)
            (_localVariableTypes ??= new Dictionary<string, Type>(_config.Comparer))[name] = type;
        else
            _concurrentVariableTypes![name] = type;
    }

    private bool IsLocalReadOnly(string name)
    {
        if (_usesLocalStore)
            return _localReadOnlyVariables != null && _localReadOnlyVariables.TryGetValue(name, out var isReadOnly) && isReadOnly;

        return _concurrentReadOnlyVariables!.TryGetValue(name, out var concurrentIsReadOnly) && concurrentIsReadOnly;
    }

    private void SetLocalReadOnly(string name, bool isReadOnly)
    {
        if (_usesLocalStore)
            (_localReadOnlyVariables ??= new Dictionary<string, bool>(_config.Comparer))[name] = isReadOnly;
        else
            _concurrentReadOnlyVariables![name] = isReadOnly;
    }
}

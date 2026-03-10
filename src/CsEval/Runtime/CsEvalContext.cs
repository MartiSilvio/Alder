using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Dynamic;
using CsEval.Diagnostics;

namespace CsEval.Runtime;

/// <summary>
/// Thread-safe evaluation context for CsEval expressions.
/// Uses ConcurrentDictionary for thread-safe variable access across parent/child relationships.
/// Parent contexts are never modified by child evaluations - children have isolated state.
/// </summary>
internal sealed class CsEvalContext
{
    private readonly ConcurrentDictionary<string, object?>? _concurrentVariables;
    private readonly ConcurrentDictionary<string, Type>? _concurrentVariableTypes;
    private readonly Dictionary<string, object?>? _localVariables;
    private readonly Dictionary<string, Type>? _localVariableTypes;
    private readonly CsEvalContext? _parent;
    private readonly CsEvalConfig _config;
    private int _variableTypeVersion;

    public CsEvalContext(CsEvalConfig config) : this(config, null, null, useConcurrentStore: true)
    {
    }

    public CsEvalContext(CsEvalConfig config, IServiceProvider? serviceProvider) : this(config, null, serviceProvider, useConcurrentStore: true)
    {
    }

    private CsEvalContext(CsEvalConfig config, CsEvalContext? parent, IServiceProvider? serviceProvider, bool useConcurrentStore)
    {
        _config = config;
        _parent = parent;
        ServiceProvider = serviceProvider ?? parent?.ServiceProvider;
        if (useConcurrentStore)
        {
            _concurrentVariables = new ConcurrentDictionary<string, object?>(_config.Comparer);
            _concurrentVariableTypes = new ConcurrentDictionary<string, Type>(_config.Comparer);
        }
        else
        {
            _localVariables = new Dictionary<string, object?>(_config.Comparer);
            _localVariableTypes = new Dictionary<string, Type>(_config.Comparer);
        }
    }

    public CsEvalConfig Config => _config;
    public StringComparer Comparer => _config.Comparer;
    public IServiceProvider? ServiceProvider { get; }
    internal TypeMetadataProvider TypeMetadata => _config.TypeMetadata;
    internal TypeResolver TypeResolver => _config.TypeResolver;
    internal FrozenDictionary<string, Func<object?[], object?>> Functions => _config.Functions;
    internal FrozenDictionary<string, ModuleInfo> Modules => _config.Modules;
    internal ImmutableArray<Type> ExtensionTypes => _config.ExtensionTypes;
    internal ExecutionConstraintState? ConstraintState { get; set; }

    public void Define(string name, object? value) => SetLocalVariable(name, value);

    public void Define(string name, object? value, Type inferredType)
    {
        SetLocalVariable(name, value);
        SetLocalVariableType(name, inferredType);
        Interlocked.Increment(ref _variableTypeVersion);
    }

    /// <summary>
    /// Defines a new variable, enforcing C# shadowing rules.
    /// Throws if the variable already exists in the current scope or any parent scope.
    /// </summary>
    public void DefineNew(string name, object? value, Type inferredType)
    {
        if (Contains(name))
            throw new CsEvalException(DiagnosticDescriptors.DuplicateLocalVariable, name);

        SetLocalVariable(name, value);
        SetLocalVariableType(name, inferredType);
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
        throw new CsEvalException(DiagnosticDescriptors.NameNotInContext, name);
    }

    public void Set(string name, object? value)
    {
        if (ContainsLocal(name))
        {
            SetLocalVariable(name, value);
            return;
        }

        if (_parent != null && _parent.Contains(name))
        {
            _parent.Set(name, value);
            return;
        }

        throw new CsEvalException(DiagnosticDescriptors.NameNotInContext, name);
    }

    private bool Contains(string name)
    {
        if (ContainsLocal(name))
            return true;
        return _parent?.Contains(name) ?? false;
    }

    public CsEvalContext CreateChild()
    {
        var child = new CsEvalContext(_config, this, null, useConcurrentStore: false);
        child.ConstraintState = ConstraintState;
        return child;
    }

    internal void ClearScope()
    {
        if (_localVariables != null)
            _localVariables.Clear();
        else
            _concurrentVariables!.Clear();

        if (_localVariableTypes != null)
            _localVariableTypes.Clear();
        else
            _concurrentVariableTypes!.Clear();

        Interlocked.Increment(ref _variableTypeVersion);
    }

    public IReadOnlyDictionary<string, object?> GetAll() =>
        _localVariables ?? (IReadOnlyDictionary<string, object?>)_concurrentVariables!;

    internal int GetTypeInferenceVersion()
    {
        var version = _variableTypeVersion;
        if (_parent != null)
            version = HashCode.Combine(version, _parent.GetTypeInferenceVersion());
        return version;
    }

    public static CsEvalContext FromExpandoObject(ExpandoObject? expando, CsEvalConfig config)
    {
        var ctx = new CsEvalContext(config);
        if (expando == null) return ctx;

        foreach (var kvp in (IDictionary<string, object?>)expando)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }

    public static CsEvalContext FromDictionary(IDictionary<string, object?>? dict, CsEvalConfig config)
    {
        var ctx = new CsEvalContext(config);
        if (dict == null) return ctx;

        foreach (var kvp in dict)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }

    private bool TryGetLocalVariable(string name, out object? value)
    {
        if (_localVariables != null)
            return _localVariables.TryGetValue(name, out value);

        return _concurrentVariables!.TryGetValue(name, out value);
    }

    private bool TryGetLocalVariableType(string name, out Type type)
    {
        if (_localVariableTypes != null)
            return _localVariableTypes.TryGetValue(name, out type!);

        return _concurrentVariableTypes!.TryGetValue(name, out type!);
    }

    private bool ContainsLocal(string name)
    {
        if (_localVariables != null)
            return _localVariables.ContainsKey(name);

        return _concurrentVariables!.ContainsKey(name);
    }

    private void SetLocalVariable(string name, object? value)
    {
        if (_localVariables != null)
            _localVariables[name] = value;
        else
            _concurrentVariables![name] = value;
    }

    private void SetLocalVariableType(string name, Type type)
    {
        if (_localVariableTypes != null)
            _localVariableTypes[name] = type;
        else
            _concurrentVariableTypes![name] = type;
    }
}

using System.Dynamic;

namespace CsEval.Evaluation;

public sealed class CsEvalContext
{
    private readonly Dictionary<string, object?> _variables;
    private readonly CsEvalContext? _parent;
    private readonly StringComparer _comparer;
    private readonly TypeCache _typeCache;

    public CsEvalContext(StringComparer? comparer = null) : this(null, comparer, null)
    {
    }

    internal CsEvalContext(StringComparer? comparer, TypeCache? typeCache) : this(null, comparer, typeCache)
    {
    }

    private CsEvalContext(CsEvalContext? parent, StringComparer? comparer, TypeCache? typeCache)
    {
        _parent = parent;
        _comparer = comparer ?? parent?._comparer ?? StringComparer.Ordinal;
        _typeCache = typeCache ?? parent?._typeCache ?? new TypeCache();
        _variables = new Dictionary<string, object?>(_comparer);
    }

    public StringComparer Comparer => _comparer;

    /// <summary>
    /// The TypeCache instance for reflection caching. Shared with child contexts.
    /// </summary>
    internal TypeCache TypeCache => _typeCache;

    public void Define(string name, object? value) => _variables[name] = value;

    public bool TryGet(string name, out object? value)
    {
        if (_variables.TryGetValue(name, out value))
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
        throw new CsEvalException($"Undefined variable '{name}'");
    }

    public void Set(string name, object? value)
    {
        if (_variables.ContainsKey(name))
        {
            _variables[name] = value;
            return;
        }

        if (_parent != null && _parent.Contains(name))
        {
            _parent.Set(name, value);
            return;
        }

        throw new CsEvalException($"Undefined variable '{name}'");
    }

    private bool Contains(string name)
    {
        if (_variables.ContainsKey(name))
            return true;
        return _parent?.Contains(name) ?? false;
    }

    public CsEvalContext CreateChild() => new(this, _comparer, _typeCache);

    public IReadOnlyDictionary<string, object?> GetAll() => _variables;

    public static CsEvalContext FromExpandoObject(ExpandoObject? expando, StringComparer? comparer = null)
    {
        return FromExpandoObject(expando, comparer, null);
    }

    internal static CsEvalContext FromExpandoObject(ExpandoObject? expando, StringComparer? comparer, TypeCache? typeCache)
    {
        var ctx = new CsEvalContext(comparer, typeCache);
        if (expando == null) return ctx;

        foreach (var kvp in (IDictionary<string, object?>)expando)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }

    public static CsEvalContext FromDictionary(IDictionary<string, object?>? dict, StringComparer? comparer = null)
    {
        return FromDictionary(dict, comparer, null);
    }

    internal static CsEvalContext FromDictionary(IDictionary<string, object?>? dict, StringComparer? comparer, TypeCache? typeCache)
    {
        var ctx = new CsEvalContext(comparer, typeCache);
        if (dict == null) return ctx;

        foreach (var kvp in dict)
        {
            ctx.Define(kvp.Key, kvp.Value);
        }
        return ctx;
    }
}
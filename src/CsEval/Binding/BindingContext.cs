using CsEval.Runtime;

namespace CsEval.Binding;

internal sealed class BindingContext
{
    private readonly CsEvalContext _context;
    private readonly BindingContext? _parent;
    private readonly Dictionary<string, Type> _locals;
    private readonly HashSet<string> _readOnlyLocals;

    public BindingContext(CsEvalContext context)
        : this(context, parent: null)
    {
    }

    private BindingContext(CsEvalContext context, BindingContext? parent)
    {
        _context = context;
        _parent = parent;
        _locals = new Dictionary<string, Type>(context.Comparer);
        _readOnlyLocals = new HashSet<string>(context.Comparer);
    }

    internal CsEvalContext RuntimeContext => _context;
    internal bool IsCaseSensitive => ReferenceEquals(_context.Comparer, StringComparer.Ordinal);
    internal BindingContext CreateChildScope() => new(_context, this);

    internal void DeclareLocal(string name, Type type, bool isReadOnly = false)
    {
        _locals[name] = type;
        if (isReadOnly)
            _readOnlyLocals.Add(name);
        else
            _readOnlyLocals.Remove(name);
    }

    internal bool IsReadOnlyLocal(string name)
    {
        if (_locals.ContainsKey(name))
            return _readOnlyLocals.Contains(name);

        return _parent?.IsReadOnlyLocal(name) ?? false;
    }

    private bool TryGetLocalType(string name, out Type type)
    {
        if (_locals.TryGetValue(name, out type!))
            return true;

        if (_parent != null)
            return _parent.TryGetLocalType(name, out type);

        type = typeof(object);
        return false;
    }

    public bool TryGetVariableType(string name, out Type type)
    {
        if (TryGetLocalType(name, out type))
            return true;

        if (_context.TryGetVariableType(name, out var declaredType) && declaredType != null)
        {
            type = declaredType;
            return true;
        }

        if (_context.TryGet(name, out var fallbackValue) && fallbackValue != null)
        {
            type = fallbackValue.GetType();
            return true;
        }

        type = typeof(object);
        return false;
    }
}

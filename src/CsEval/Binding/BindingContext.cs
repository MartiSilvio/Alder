using CsEval.Runtime;

namespace CsEval.Binding;

internal sealed class BindingContext
{
    private readonly CsEvalContext _context;
    private readonly BindingContext? _parent;
    private readonly Dictionary<string, Type> _locals;

    public BindingContext(CsEvalContext context)
        : this(context, parent: null)
    {
    }

    private BindingContext(CsEvalContext context, BindingContext? parent)
    {
        _context = context;
        _parent = parent;
        _locals = new Dictionary<string, Type>(context.Comparer);
    }

    internal CsEvalContext RuntimeContext => _context;
    internal bool IsCaseSensitive => _context.Comparer == StringComparer.Ordinal;
    internal BindingContext CreateChildScope() => new(_context, this);

    internal void DeclareLocal(string name, Type type)
    {
        _locals[name] = type;
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

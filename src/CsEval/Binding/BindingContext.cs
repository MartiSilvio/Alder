using CsEval.Runtime;

namespace CsEval.Binding;

internal sealed class BindingContext
{
    private readonly CsEvalContext _context;

    public BindingContext(CsEvalContext context)
    {
        _context = context;
    }

    internal CsEvalContext RuntimeContext => _context;
    internal bool IsCaseSensitive => _context.Comparer == StringComparer.Ordinal;

    public bool TryGetVariableType(string name, out Type type)
    {
        if (_context.TryGetVariableType(name, out var declaredType) && declaredType != null)
        {
            if (declaredType == typeof(object) && _context.TryGet(name, out var runtimeValue) && runtimeValue != null)
            {
                type = runtimeValue.GetType();
                return true;
            }

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

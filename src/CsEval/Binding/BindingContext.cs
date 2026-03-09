using CsEval.Runtime;

namespace CsEval.Binding;

internal sealed class BindingContext
{
    private readonly CsEvalContext _context;

    public BindingContext(CsEvalContext context)
    {
        _context = context;
    }

    public bool TryGetVariableType(string name, out Type type)
    {
        if (_context.TryGetVariableType(name, out var declaredType) && declaredType != null)
        {
            type = declaredType;
            return true;
        }

        if (_context.TryGet(name, out var runtimeValue) && runtimeValue != null)
        {
            type = runtimeValue.GetType();
            return true;
        }

        type = typeof(object);
        return false;
    }
}

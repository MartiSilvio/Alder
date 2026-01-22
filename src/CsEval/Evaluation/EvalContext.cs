using System.Dynamic;

namespace CsEval.Evaluation
{
    public sealed class EvalContext
    {
        private readonly Dictionary<string, object?> _variables;
        private readonly EvalContext? _parent;
        private readonly StringComparer _comparer;

        public EvalContext(StringComparer? comparer = null) : this(null, comparer)
        {
        }

        private EvalContext(EvalContext? parent, StringComparer? comparer)
        {
            _parent = parent;
            _comparer = comparer ?? parent?._comparer ?? StringComparer.Ordinal;
            _variables = new Dictionary<string, object?>(_comparer);
        }

        public StringComparer Comparer => _comparer;

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
            throw new EvalException($"Undefined variable '{name}'");
        }

        public EvalContext CreateChild() => new(this, _comparer);

        public static EvalContext FromExpandoObject(ExpandoObject? expando, StringComparer? comparer = null)
        {
            var ctx = new EvalContext(comparer);
            if (expando == null) return ctx;

            foreach (var kvp in (IDictionary<string, object?>)expando)
            {
                ctx.Define(kvp.Key, kvp.Value);
            }
            return ctx;
        }

        public static EvalContext FromDictionary(IDictionary<string, object?>? dict, StringComparer? comparer = null)
        {
            var ctx = new EvalContext(comparer);
            if (dict == null) return ctx;

            foreach (var kvp in dict)
            {
                ctx.Define(kvp.Key, kvp.Value);
            }
            return ctx;
        }
    }
}

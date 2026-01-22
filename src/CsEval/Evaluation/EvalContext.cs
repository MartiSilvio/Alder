using System.Dynamic;

namespace CsEval.Evaluation
{
    public sealed class EvalContext
    {
        private readonly Dictionary<string, object?> _variables = new(StringComparer.OrdinalIgnoreCase);
        private readonly EvalContext? _parent;

        public EvalContext(EvalContext? parent = null)
        {
            _parent = parent;
        }

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

        public EvalContext CreateChild() => new(this);

        public static EvalContext FromExpandoObject(ExpandoObject? expando)
        {
            var ctx = new EvalContext();
            if (expando == null) return ctx;

            foreach (var kvp in (IDictionary<string, object?>)expando)
            {
                ctx.Define(kvp.Key, kvp.Value);
            }
            return ctx;
        }

        public static EvalContext FromDictionary(IDictionary<string, object?>? dict)
        {
            var ctx = new EvalContext();
            if (dict == null) return ctx;

            foreach (var kvp in dict)
            {
                ctx.Define(kvp.Key, kvp.Value);
            }
            return ctx;
        }
    }
}

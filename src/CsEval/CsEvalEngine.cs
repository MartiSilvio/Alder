using CsEval.Evaluation;
using CsEval.Parsing;

namespace CsEval
{
    public sealed class CsEvalEngine
    {
        private readonly EvalContext _context;
        private readonly Dictionary<string, Func<object?[], object?>> _functions;
        private readonly CsEvalOptions _options;

        public CsEvalEngine() : this(CsEvalOptions.Default)
        {
        }

        public CsEvalEngine(CsEvalOptions options)
        {
            _options = options;
            _context = new EvalContext(options.StringComparer);
            _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
            RegisterStaticProxies();
        }

        public object? Evaluate(string expression)
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();

            var parser = new Parser(tokens);
            var ast = parser.Parse();

            var evaluator = new Evaluator(_context, _functions, _options);
            return evaluator.Evaluate(ast);
        }

        public T? Evaluate<T>(string expression)
        {
            var result = Evaluate(expression);

            if (result == null)
                return default;

            if (result is T typed)
                return typed;

            return (T)Convert.ChangeType(result, typeof(T));
        }

        public CsEvalEngine SetVariable(string name, object? value)
        {
            _context.Define(name, value);
            return this;
        }

        public CsEvalEngine SetVariables(IDictionary<string, object?> variables)
        {
            foreach (var (name, value) in variables)
            {
                _context.Define(name, value);
            }
            return this;
        }

        public CsEvalEngine RegisterFunction(string name, Func<object?[], object?> function)
        {
            _functions[name] = function;
            return this;
        }

        public CsEvalEngine RegisterProxy(string name, object proxy)
        {
            _context.Define(name, proxy);
            return this;
        }

        private void RegisterStaticProxies()
        {
            _context.Define("Math", new MathProxy());
            _context.Define("DateTime", new DateTimeProxy());
            _context.Define("Guid", new GuidProxy());
            _context.Define("Convert", new ConvertProxy());
            _context.Define("String", new StringProxy());
            _context.Define("Enumerable", new EnumerableProxy());
            _context.Define("Console", new ConsoleProxy());
        }
    }
}

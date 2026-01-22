using System.Reflection;
using CsEval.Attributes;
using CsEval.Evaluation;
using CsEval.Parsing;

namespace CsEval;

public sealed class CsEvalEngine
{
    private readonly EvalContext _context;
    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly List<RegisteredType> _registeredTypes = [];

    public CsEvalEngine() : this(CsEvalOptions.Default)
    {
    }

    public CsEvalEngine(CsEvalOptions options)
    {
        _options = options;
        _context = new EvalContext(options.StringComparer);
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        RegisterBuiltInProxies();
    }

    public CsEvalExpression Parse(string expression)
    {
        var lexer = new Lexer(expression);
        var tokens = lexer.Tokenize();

        var parser = new Parser(tokens);
        var ast = parser.Parse();

        return new CsEvalExpression(expression, ast);
    }

    public object? Evaluate(string expression, IServiceProvider? serviceProvider = null)
    {
        return Evaluate(expression, serviceProvider, CancellationToken.None);
    }

    public object? Evaluate(string expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
        var parsed = Parse(expression);
        return Evaluate(parsed, serviceProvider, cancellationToken);
    }

    public object? Evaluate(CsEvalExpression expression, IServiceProvider? serviceProvider = null)
    {
        return Evaluate(expression, serviceProvider, CancellationToken.None);
    }

    public object? Evaluate(CsEvalExpression expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
        ApplyRegisteredTypes(serviceProvider);

        var evaluator = new Evaluator(_context, _functions, _options, cancellationToken);
        return evaluator.Evaluate(expression.Ast);
    }

    public Task<object?> EvaluateAsync(string expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Evaluate(expression, serviceProvider, cancellationToken), cancellationToken);
    }

    public Task<object?> EvaluateAsync(CsEvalExpression expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Evaluate(expression, serviceProvider, cancellationToken), cancellationToken);
    }

    public T? Evaluate<T>(string expression, IServiceProvider? serviceProvider = null)
    {
        return Evaluate<T>(expression, serviceProvider, CancellationToken.None);
    }

    public T? Evaluate<T>(string expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
        var result = Evaluate(expression, serviceProvider, cancellationToken);

        if (result == null)
            return default;

        if (result is T typed)
            return typed;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<T?> EvaluateAsync<T>(string expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(expression, serviceProvider, cancellationToken).ConfigureAwait(false);

        if (result == null)
            return default;

        if (result is T typed)
            return typed;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public T? Evaluate<T>(CsEvalExpression expression, IServiceProvider? serviceProvider = null)
    {
        return Evaluate<T>(expression, serviceProvider, CancellationToken.None);
    }

    public T? Evaluate<T>(CsEvalExpression expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
        var result = Evaluate(expression, serviceProvider, cancellationToken);

        if (result == null)
            return default;

        if (result is T typed)
            return typed;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<T?> EvaluateAsync<T>(CsEvalExpression expression, IServiceProvider? serviceProvider = null, CancellationToken cancellationToken = default)
    {
        var result = await EvaluateAsync(expression, serviceProvider, cancellationToken).ConfigureAwait(false);

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

    public CsEvalEngine RegisterFromAssembly(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var isModule = type.GetCustomAttribute<CsEvalModuleAttribute>() != null;
            var hasGlobalFunctions = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Any(m => m.GetCustomAttribute<CsEvalFunctionAttribute>() != null);

            if (!isModule && !hasGlobalFunctions)
                continue;

            // For non-static types without parameterless constructors, skip during assembly scan
            // (they can still be registered explicitly with an instance or via DI)
            var hasStaticOnly = isModule
                ? false
                : type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .All(m => m.GetCustomAttribute<CsEvalFunctionAttribute>() != null);

            if (!hasStaticOnly && type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            _registeredTypes.Add(new RegisteredType(type, null, FromAssemblyScan: true));
        }
        return this;
    }

    public CsEvalEngine RegisterFromType(Type type, object? instance = null)
    {
        _registeredTypes.Add(new RegisteredType(type, instance, FromAssemblyScan: false));
        return this;
    }

    public CsEvalEngine RegisterFromType<T>(T? instance = default) where T : class
    {
        return RegisterFromType(typeof(T), instance);
    }

    private void ApplyRegisteredTypes(IServiceProvider? serviceProvider)
    {
        foreach (var reg in _registeredTypes)
        {
            var moduleAttr = reg.Type.GetCustomAttribute<CsEvalModuleAttribute>();

            if (moduleAttr != null)
            {
                var proxy = ResolveInstance(reg, serviceProvider);
                _context.Define(moduleAttr.Name, proxy);
            }
            else
            {
                RegisterGlobalFunctions(reg, serviceProvider);
            }
        }
    }

    private object ResolveInstance(RegisteredType reg, IServiceProvider? serviceProvider)
    {
        if (reg.Instance != null)
            return reg.Instance;

        var resolved = serviceProvider?.GetService(reg.Type);
        if (resolved != null)
            return resolved;

        return Activator.CreateInstance(reg.Type)
               ?? throw new InvalidOperationException($"Cannot create instance of '{reg.Type.FullName}'");
    }

    private void RegisterGlobalFunctions(RegisteredType reg, IServiceProvider? serviceProvider)
    {
        var methods = reg.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<CsEvalFunctionAttribute>();
            if (attr == null) continue;

            object? target = null;
            if (!method.IsStatic)
            {
                target = ResolveInstance(reg, serviceProvider);
            }

            _functions[attr.Name] = CreateFunctionDelegate(method, target);
        }
    }

    private static Func<object?[], object?> CreateFunctionDelegate(MethodInfo method, object? target)
    {
        return args =>
        {
            var parameters = method.GetParameters();
            var convertedArgs = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                if (i < args.Length)
                {
                    convertedArgs[i] = ConvertArgument(args[i], parameters[i].ParameterType);
                }
                else if (parameters[i].HasDefaultValue)
                {
                    convertedArgs[i] = parameters[i].DefaultValue;
                }
                else
                {
                    throw new ArgumentException($"Missing required argument '{parameters[i].Name}'");
                }
            }

            return method.Invoke(target, convertedArgs);
        };
    }

    private static object? ConvertArgument(object? arg, Type targetType)
    {
        if (arg == null)
            return null;

        if (targetType.IsInstanceOfType(arg))
            return arg;

        return Convert.ChangeType(arg, targetType);
    }

    private void RegisterBuiltInProxies()
    {
        _context.Define("Math", new MathProxy());
        _context.Define("DateTime", new DateTimeProxy());
        _context.Define("Guid", new GuidProxy());
        _context.Define("Convert", new ConvertProxy());
        _context.Define("String", new StringProxy());
        _context.Define("Enumerable", new EnumerableProxy());
        _context.Define("Console", new ConsoleProxy());
    }

    private sealed record RegisteredType(Type Type, object? Instance, bool FromAssemblyScan);
}
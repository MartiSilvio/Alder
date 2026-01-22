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

    public Func<MethodInfo, object?[], object?[]>? ArgumentTransformer { get; set; }

    public CsEvalEngine() : this(CsEvalOptions.Default)
    {
    }

    public CsEvalEngine(CsEvalOptions options)
    {
        _options = options;
        _context = new EvalContext(options.StringComparer);
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        RegisterBuiltInModules();
    }

    private CsEvalEngine(EvalContext context, Dictionary<string, Func<object?[], object?>> functions,
        List<RegisteredType> registeredTypes, CsEvalOptions options)
    {
        _context = context;
        _functions = functions;
        _registeredTypes = registeredTypes;
        _options = options;
    }

    public CsEvalExpression Parse(string expression)
    {
        var lexer = new Lexer(expression);
        var tokens = lexer.Tokenize();

        var parser = new Parser(tokens);
        var ast = parser.Parse();

        return new CsEvalExpression(expression, ast);
    }

    public bool TryParse(string expression, out CsEvalExpression? result, out string? error)
    {
        try
        {
            result = Parse(expression);
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            result = null;
            error = ex.Message;
            return false;
        }
    }

    public bool TryParse(string expression, out CsEvalExpression? result)
    {
        return TryParse(expression, out result, out _);
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

    public object? Evaluate(CsEvalExpression expression, IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        ApplyRegisteredTypes(serviceProvider);

        var evaluator = new Evaluator(_context, _functions, _options, cancellationToken, ArgumentTransformer);
        return evaluator.Evaluate(expression.Ast);
    }

    public CsEvalEngine CreateChild()
    {
        return new CsEvalEngine(_context.CreateChild(), _functions, _registeredTypes, _options);
    }

    public Task<object?> EvaluateAsync(string expression, IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Evaluate(expression, serviceProvider, cancellationToken), cancellationToken);
    }

    public Task<object?> EvaluateAsync(CsEvalExpression expression, IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
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

    public async Task<T?> EvaluateAsync<T>(string expression, IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
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

    public T? Evaluate<T>(CsEvalExpression expression, IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        var result = Evaluate(expression, serviceProvider, cancellationToken);

        if (result == null)
            return default;

        if (result is T typed)
            return typed;

        return (T)Convert.ChangeType(result, typeof(T));
    }

    public async Task<T?> EvaluateAsync<T>(CsEvalExpression expression, IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
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

            var hasStaticOnly = !isModule &&
                                type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                                    .All(m => m.GetCustomAttribute<CsEvalFunctionAttribute>() != null);

            if (!hasStaticOnly && type.GetConstructor(Type.EmptyTypes) == null)
                continue;

            _registeredTypes.Add(new RegisteredType(type, null, null, BuildMemberDictionary(type), FromAssemblyScan: true));
        }

        return this;
    }

    public CsEvalEngine RegisterFromType(Type type, object? instance = null)
    {
        _registeredTypes.Add(new RegisteredType(type, instance, null, BuildMemberDictionary(type), FromAssemblyScan: false));
        return this;
    }

    public CsEvalEngine RegisterFromType<T>(T? instance = default) where T : class
    {
        return RegisterFromType(typeof(T), instance);
    }

    public CsEvalEngine RegisterModule(string moduleName, Type type)
    {
        var methods = BuildMemberDictionary(type);
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, methods, FromAssemblyScan: false));
        return this;
    }

    public CsEvalEngine RegisterModule<T>(string moduleName, T? instance = default) where T : class
    {
        var methods = BuildMemberDictionary(typeof(T));
        _registeredTypes.Add(new RegisteredType(typeof(T), instance, moduleName, methods, FromAssemblyScan: false));
        return this;
    }

    public CsEvalEngine RegisterModule(string moduleName, Type type, IReadOnlyDictionary<string, MemberInfo> members)
    {
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, members, FromAssemblyScan: false));
        return this;
    }

    private IReadOnlyDictionary<string, MemberInfo> BuildMemberDictionary(Type type)
    {
        var members = new Dictionary<string, MemberInfo>(_options.StringComparer);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName)
                continue;
            members[method.Name] = method;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            members[prop.Name] = prop;
        }

        return members;
    }

    public IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
    {
        var result = new Dictionary<string, RegisteredModule>(_options.StringComparer);

        foreach (var reg in _registeredTypes)
        {
            var moduleName = reg.ModuleName ?? reg.Type.GetCustomAttribute<CsEvalModuleAttribute>()?.Name;
            if (moduleName == null)
                continue;

            result[moduleName] = new RegisteredModule(reg.Type, reg.Instance, reg.Members);
        }

        return result;
    }

    public sealed record RegisteredModule(Type Type, object? Instance, IReadOnlyDictionary<string, MemberInfo>? Members);

    public sealed class ModuleResolver
    {
        public Type Type { get; }
        public object? Instance { get; }
        public IServiceProvider? ServiceProvider { get; }
        public IReadOnlyDictionary<string, MemberInfo> Members { get; }

        public ModuleResolver(Type type, object? instance, IServiceProvider? serviceProvider,
            IReadOnlyDictionary<string, MemberInfo> members)
        {
            Type = type;
            Instance = instance;
            ServiceProvider = serviceProvider;
            Members = members;
        }

        public object Resolve()
        {
            if (Instance != null)
                return Instance;

            if (ServiceProvider != null)
            {
                var resolved = ServiceProvider.GetService(Type);
                if (resolved != null)
                    return resolved;
            }

            if (Type.GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance(Type)
                       ?? throw new InvalidOperationException($"Cannot create instance of '{Type.FullName}'");
            }

            throw new InvalidOperationException(
                $"Cannot resolve instance of '{Type.FullName}'. " +
                $"Either register it in IServiceProvider or ensure it has a parameterless constructor.");
        }
    }

    private void ApplyRegisteredTypes(IServiceProvider? serviceProvider)
    {
        foreach (var reg in _registeredTypes)
        {
            var moduleName = reg.ModuleName ?? reg.Type.GetCustomAttribute<CsEvalModuleAttribute>()?.Name;

            if (moduleName != null)
            {
                _context.Define(moduleName, new ModuleResolver(reg.Type, reg.Instance, serviceProvider, reg.Members));
            }
            else
            {
                RegisterGlobalFunctions(reg, serviceProvider);
            }
        }
    }

    private void RegisterGlobalFunctions(RegisteredType reg, IServiceProvider? serviceProvider)
    {
        var methods = reg.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<CsEvalFunctionAttribute>();
            if (attr == null) continue;

            var resolver = method.IsStatic ? null : new ModuleResolver(reg.Type, reg.Instance, serviceProvider, reg.Members);
            _functions[attr.Name] = CreateFunctionDelegate(method, resolver);
        }
    }

    private static Func<object?[], object?> CreateFunctionDelegate(MethodInfo method, ModuleResolver? resolver)
    {
        return args =>
        {
            var parameters = method.GetParameters();
            var finalArgs = PadWithDefaults(parameters, args);
            return method.Invoke(resolver?.Resolve(), finalArgs);
        };
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args)
    {
        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
            {
                result[i] = CoerceNumeric(args[i], parameters[i].ParameterType);
            }
            else if (parameters[i].HasDefaultValue)
            {
                result[i] = parameters[i].DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Missing required argument '{parameters[i].Name}'");
            }
        }

        return result;
    }

    private static object? CoerceNumeric(object? arg, Type targetType)
    {
        if (arg == null) return null;
        if (targetType.IsInstanceOfType(arg)) return arg;

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (arg is IConvertible && IsNumericType(underlying))
            return Convert.ChangeType(arg, underlying);

        return arg;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(double) ||
        type == typeof(float) || type == typeof(decimal) || type == typeof(short) ||
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(ushort) ||
        type == typeof(uint) || type == typeof(ulong);

    private void RegisterBuiltInModules()
    {
        RegisterModule("Math", new MathProxy());
        RegisterModule("DateTime", new DateTimeProxy());
        RegisterModule("Guid", new GuidProxy());
        RegisterModule("Convert", new ConvertProxy());
        RegisterModule("String", new StringProxy());
        RegisterModule("Enumerable", new EnumerableProxy());
    }

    private sealed record RegisteredType(
        Type Type,
        object? Instance,
        string? ModuleName,
        IReadOnlyDictionary<string, MemberInfo> Members,
        bool FromAssemblyScan);
}
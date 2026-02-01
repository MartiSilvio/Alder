using CsEval.Attributes;
using CsEval.Compilation;
using CsEval.Interpretation;
using CsEval.Modules;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval;

public sealed class CsEvalEngine
{
    private readonly CsEvalContext _context;
    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly List<RegisteredType> _registeredTypes = [];
    private readonly TypeCache _typeCache;
    private readonly ExpressionCache _expressionCache;

    public Func<MethodInfo, object?[], object?[]>? ArgumentTransformer { get; set; }

    public CsEvalEngine() : this(CsEvalOptions.Default)
    {
    }

    public CsEvalEngine(CsEvalOptions options)
    {
        _options = options;
        _typeCache = new TypeCache();
        _expressionCache = new ExpressionCache();
        _context = new CsEvalContext(options.StringComparer, _typeCache);
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        RegisterBuiltInModules();
    }

    public CsEvalEngine(CsEvalOptions options, CsEvalContext context)
    {
        _options = options;
        _context = context;
        _typeCache = context.TypeCache;
        _expressionCache = new ExpressionCache();
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        RegisterBuiltInModules();
    }

    private CsEvalEngine(CsEvalContext context, Dictionary<string, Func<object?[], object?>> functions,
        List<RegisteredType> registeredTypes, CsEvalOptions options, TypeCache typeCache, ExpressionCache expressionCache)
    {
        _context = context;
        _functions = functions;
        _registeredTypes = registeredTypes;
        _options = options;
        _typeCache = typeCache;
        _expressionCache = expressionCache;
    }

    public CsEvalExpression Parse(string expression)
    {
        var lexer = new Lexer(expression);
        var tokens = lexer.Tokenize();

        var parser = new Parser(tokens);
        var ast = parser.Parse();

        return new CsEvalExpression(expression, ast, _expressionCache);
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

        var shouldCompile = _options.CompilationMode is CompilationMode.Compiled or CompilationMode.StrictCompiled;
        if (shouldCompile && expression.GetCompiledInfo() == null)
        {
            expression.TryCompile();
        }

        // Use compiled delegate if available
        var compiled = expression.GetCompiledInfo();
        if (compiled?.Delegate != null)
        {
            return compiled.Delegate(_context, _options, cancellationToken, _functions, ArgumentTransformer);
        }

        // StrictCompiled mode: throw if compilation failed
        if (_options.CompilationMode == CompilationMode.StrictCompiled)
        {
            var reason = compiled?.FailureReason ?? "Unknown compilation failure";
            throw new CsEvalException($"Expression could not be compiled to IL: {reason}");
        }

        // Fall back to tree-walking for non-compilable expressions
        var evaluator = new Evaluator(_context, _functions, _options, cancellationToken, ArgumentTransformer);
        return evaluator.Evaluate(expression.Ast);
    }

    /// <summary>
    /// Parses and attempts to compile an expression upfront for better performance.
    /// If compilation is not possible, the expression will fall back to tree-walking on evaluation.
    /// </summary>
    /// <remarks>
    /// Use this method when you want to ensure the expression is compiled regardless
    /// of the <see cref="CsEvalOptions.CompilationMode"/> setting, or when you want
    /// to pay the compilation cost upfront (e.g., during app startup).
    /// </remarks>
    /// <param name="expression">The expression string to parse and compile.</param>
    /// <returns>A pre-parsed and potentially compiled expression.</returns>
    public CsEvalExpression ParseAndCompile(string expression)
    {
        var expr = Parse(expression);
        expr.TryCompile();
        return expr;
    }

    /// <summary>
    /// Creates a child engine with an isolated evaluation context.
    /// The child inherits variables from the parent (read-only) but has its own scope for new variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Thread Safety:</b> CsEvalEngine is not thread-safe. For concurrent evaluation,
    /// each thread must use its own child context created via this method.
    /// </para>
    /// <example>
    /// <code>
    /// // Concurrent evaluation pattern
    /// Parallel.ForEach(items, item => {
    ///     var child = engine.CreateChild();
    ///     child.SetVariable("item", item);
    ///     child.Evaluate(expression);
    /// });
    /// </code>
    /// </example>
    /// </remarks>
    /// <returns>A new CsEvalEngine with an isolated child context.</returns>
    public CsEvalEngine CreateChild()
    {
        return new CsEvalEngine(_context.CreateChild(), _functions, _registeredTypes, _options, _typeCache, _expressionCache);
    }

    public object? Evaluate(string expression, IDictionary<string, object?> variables, IServiceProvider? serviceProvider = null)
    {
        return Evaluate(expression, variables, serviceProvider, CancellationToken.None);
    }

    public object? Evaluate(
        string expression, IDictionary<string, object?> variables, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
        var child = CreateChild();
        child.SetVariables(variables);
        return child.Evaluate(expression, serviceProvider, cancellationToken);
    }

    public object? Evaluate(
        CsEvalExpression expression, IDictionary<string, object?> variables, IServiceProvider? serviceProvider = null)
    {
        return Evaluate(expression, variables, serviceProvider, CancellationToken.None);
    }

    public object? Evaluate(CsEvalExpression expression, IDictionary<string, object?> variables,
        IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
        var child = CreateChild();
        child.SetVariables(variables);
        return child.Evaluate(expression, serviceProvider, cancellationToken);
    }

    public T? Evaluate<T>(string expression, IServiceProvider? serviceProvider = null)
    {
        return Evaluate<T>(expression, serviceProvider, CancellationToken.None);
    }

    public T? Evaluate<T>(string expression, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    {
        var result = Evaluate(expression, serviceProvider, cancellationToken);

        return result switch
        {
            null => default,
            T typed => typed,
            _ => (T)Convert.ChangeType(result, typeof(T))
        };
    }



    public T? Evaluate<T>(CsEvalExpression expression, IServiceProvider? serviceProvider = null)
    {
        return Evaluate<T>(expression, serviceProvider, CancellationToken.None);
    }

    public T? Evaluate<T>(CsEvalExpression expression, IServiceProvider? serviceProvider,
        CancellationToken cancellationToken)
    {
        var result = Evaluate(expression, serviceProvider, cancellationToken);

        return result switch
        {
            null => default,
            T typed => typed,
            _ => (T)Convert.ChangeType(result, typeof(T))
        };
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
        var moduleAttr = type.GetCustomAttribute<CsEvalModuleAttribute>();
        var explicitOnly = moduleAttr?.ExplicitOnly ?? false;
        var methods = BuildMemberDictionary(type, explicitOnly);
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, methods, FromAssemblyScan: false));
        return this;
    }

    public CsEvalEngine RegisterModule<T>(string moduleName, T? instance = default) where T : class
    {
        var moduleAttr = typeof(T).GetCustomAttribute<CsEvalModuleAttribute>();
        var explicitOnly = moduleAttr?.ExplicitOnly ?? false;
        var methods = BuildMemberDictionary(typeof(T), explicitOnly);
        _registeredTypes.Add(new RegisteredType(typeof(T), instance, moduleName, methods, FromAssemblyScan: false));
        return this;
    }

    /// <summary>
    /// Registers a module with explicit control over which methods are exposed.
    /// </summary>
    /// <param name="moduleName">The name used to access the module in expressions.</param>
    /// <param name="type">The module type.</param>
    /// <param name="explicitOnly">When true, only methods marked with [CsEvalFunction] are exposed.</param>
    public CsEvalEngine RegisterModule(string moduleName, Type type, bool explicitOnly)
    {
        var methods = BuildMemberDictionary(type, explicitOnly);
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, methods, FromAssemblyScan: false));
        return this;
    }

    /// <summary>
    /// Registers a module with explicit control over which methods are exposed.
    /// </summary>
    /// <typeparam name="T">The module type.</typeparam>
    /// <param name="moduleName">The name used to access the module in expressions.</param>
    /// <param name="explicitOnly">When true, only methods marked with [CsEvalFunction] are exposed.</param>
    /// <param name="instance">Optional instance to use (for instance methods).</param>
    public CsEvalEngine RegisterModule<T>(string moduleName, bool explicitOnly, T? instance = default) where T : class
    {
        var methods = BuildMemberDictionary(typeof(T), explicitOnly);
        _registeredTypes.Add(new RegisteredType(typeof(T), instance, moduleName, methods, FromAssemblyScan: false));
        return this;
    }

    public CsEvalEngine RegisterModule(string moduleName, Type type, IReadOnlyDictionary<string, MemberInfo> members)
    {
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, members, FromAssemblyScan: false));
        return this;
    }

    private IReadOnlyDictionary<string, MemberInfo> BuildMemberDictionary(Type type, bool explicitOnly = false)
    {
        var members = new Dictionary<string, MemberInfo>(_options.StringComparer);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName)
                continue;

            // Skip async methods - they return Task/Task<T> which is not supported
            if (IsAsyncMethod(method))
                continue;

            var attr = method.GetCustomAttribute<CsEvalFunctionAttribute>();

            if (explicitOnly && attr == null)
                continue;

            // Use attribute name if specified, otherwise use method name
            var name = attr?.Name ?? method.Name;
            members[name] = method;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            // Properties are always included (they're read-only access)
            // Could add [CsEvalProperty] in the future if needed
            if (!explicitOnly)
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

            var functionName = attr.Name ?? method.Name;
            var resolver = method.IsStatic ? null : new ModuleResolver(reg.Type, reg.Instance, serviceProvider, reg.Members);
            _functions[functionName] = CreateFunctionDelegate(method, resolver);
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

    private static bool IsAsyncMethod(MethodInfo method)
    {
        var returnType = method.ReturnType;
        if (returnType == typeof(Task))
            return true;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            return true;
        if (returnType == typeof(ValueTask))
            return true;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            return true;
        return false;
    }

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
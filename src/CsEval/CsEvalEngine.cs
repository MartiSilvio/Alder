using System.Collections.Immutable;
using CsEval.Attributes;
using CsEval.Compilation;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval;

public sealed class CsEvalEngine
{
    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly List<RegisteredType> _registeredTypes = [];
    private readonly List<Type> _extensionTypes = [];
    private readonly List<Assembly> _assemblies = [];
    private readonly List<string> _usingNamespaces = [];
    private readonly TypeCache _typeCache;
    private readonly ExpressionCache _expressionCache;
    private readonly Dictionary<string, object?> _pendingVariables;

    private CsEvalConfig? _frozenConfig;
    private CsEvalContext? _context;

    public Func<MethodInfo, object?[], object?[]>? ArgumentTransformer { get; set; }

    public CsEvalEngine() : this(CsEvalOptions.Default)
    {
    }

    public CsEvalEngine(CsEvalOptions options)
    {
        _options = options;
        _typeCache = new TypeCache();
        _expressionCache = new ExpressionCache();
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        _pendingVariables = new Dictionary<string, object?>(options.StringComparer);
        _extensionTypes.Add(typeof(Enumerable));
        RegisterBuiltInModules();
    }

    private CsEvalEngine(
        CsEvalConfig frozenConfig,
        CsEvalContext parentContext,
        CsEvalOptions options,
        ExpressionCache expressionCache)
    {
        _frozenConfig = frozenConfig;
        _context = parentContext.CreateChild();
        _options = options;
        _typeCache = frozenConfig.TypeCache;
        _expressionCache = expressionCache;
        _functions = new Dictionary<string, Func<object?[], object?>>(frozenConfig.Functions, options.StringComparer);
        _pendingVariables = new Dictionary<string, object?>(options.StringComparer);
        _extensionTypes = [..frozenConfig.ExtensionTypes];
        _registeredTypes = [];
    }

    private CsEvalConfig GetOrCreateConfig()
    {
        if (_frozenConfig != null)
            return _frozenConfig;

        var modules = new Dictionary<string, ModuleInfo>(_options.StringComparer);
        foreach (var reg in _registeredTypes)
        {
            var moduleName = reg.ModuleName ?? reg.Type.GetCustomAttribute<CsEvalModuleAttribute>()?.Name;
            if (moduleName != null)
            {
                modules[moduleName] = new ModuleInfo(reg.Type, reg.Instance, reg.Members);
            }
            else
            {
                RegisterGlobalFunctions(reg);
            }
        }

        var typeResolver = TypeResolver.Create(
            _assemblies.ToImmutableArray(),
            _usingNamespaces.ToImmutableArray(),
            true,
            _options.StringComparer);
        var newConfig = CsEvalConfig.Create(_functions, modules, _extensionTypes, _typeCache, typeResolver, _options.StringComparer);
        Interlocked.CompareExchange(ref _frozenConfig, newConfig, null);
        return _frozenConfig!;
    }

    private CsEvalContext GetOrCreateContext(IServiceProvider? serviceProvider)
    {
        if (_context != null)
            return _context;

        var config = GetOrCreateConfig();
        var newContext = new CsEvalContext(config, serviceProvider);

        foreach (var (name, value) in _pendingVariables)
        {
            newContext.Define(name, value);
        }
        _pendingVariables.Clear();

        Interlocked.CompareExchange(ref _context, newContext, null);
        return _context!;
    }

    public CsEvalExpression Parse(string expression)
    {
        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();

            var parser = ExpressionParser.CreateForSubExpression(tokens);
            var ast = parser.Parse();

            return new CsEvalExpression(expression, ast, _expressionCache);
        }
        catch (System.InsufficientExecutionStackException)
        {
            throw new CsEvalException("Expression nesting depth exceeded available stack space.");
        }
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
        var context = GetOrCreateContext(serviceProvider);

        var shouldCompile = _options.CompilationMode is CompilationMode.Compiled or CompilationMode.StrictCompiled;
        if (shouldCompile && expression.GetCompiledInfo() == null)
        {
            expression.TryCompile();
        }

        var compiled = expression.GetCompiledInfo();
        if (compiled?.Delegate != null)
        {
            return compiled.Delegate(context, _options, cancellationToken, ArgumentTransformer);
        }

        if (_options.CompilationMode == CompilationMode.StrictCompiled)
        {
            var reason = compiled?.FailureReason ?? "Unknown compilation failure";
            throw new CsEvalException($"Expression could not be compiled to IL: {reason}");
        }

        var evaluator = new Evaluator(context, _options, cancellationToken, ArgumentTransformer);
        return evaluator.Evaluate(expression.Ast);
    }

    public CsEvalExpression ParseAndCompile(string expression)
    {
        var expr = Parse(expression);
        expr.TryCompile();
        return expr;
    }

    public CsEvalEngine CreateChild()
    {
        var config = GetOrCreateConfig();
        var parentContext = GetOrCreateContext(null);
        return new CsEvalEngine(config, parentContext, _options, _expressionCache);
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
        if (_context != null)
        {
            _context.Define(name, value);
        }
        else
        {
            _pendingVariables[name] = value;
        }
        return this;
    }

    public CsEvalEngine SetVariables(IDictionary<string, object?> variables)
    {
        if (_context != null)
        {
            foreach (var (name, value) in variables)
            {
                _context.Define(name, value);
            }
        }
        else
        {
            foreach (var (name, value) in variables)
            {
                _pendingVariables[name] = value;
            }
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

            _registeredTypes.Add(new RegisteredType(type, null, null, BuildMemberDictionary(type)));
        }

        return this;
    }

    public CsEvalEngine RegisterFromType(Type type, object? instance = null)
    {
        _registeredTypes.Add(new RegisteredType(type, instance, null, BuildMemberDictionary(type)));
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
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, methods));
        return this;
    }

    public CsEvalEngine RegisterModule<T>(string moduleName, T? instance = default) where T : class
    {
        var moduleAttr = typeof(T).GetCustomAttribute<CsEvalModuleAttribute>();
        var explicitOnly = moduleAttr?.ExplicitOnly ?? false;
        var methods = BuildMemberDictionary(typeof(T), explicitOnly);
        _registeredTypes.Add(new RegisteredType(typeof(T), instance, moduleName, methods));
        return this;
    }

    public CsEvalEngine RegisterModule(string moduleName, Type type, bool explicitOnly)
    {
        var methods = BuildMemberDictionary(type, explicitOnly);
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, methods));
        return this;
    }

    public CsEvalEngine RegisterModule<T>(string moduleName, bool explicitOnly, T? instance = default) where T : class
    {
        var methods = BuildMemberDictionary(typeof(T), explicitOnly);
        _registeredTypes.Add(new RegisteredType(typeof(T), instance, moduleName, methods));
        return this;
    }

    public CsEvalEngine RegisterModule(string moduleName, Type type, IReadOnlyDictionary<string, MemberInfo> members)
    {
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, members));
        return this;
    }

    public CsEvalEngine AddAssembly(Assembly assembly)
    {
        if (_frozenConfig != null)
            throw new InvalidOperationException("Cannot add assemblies after evaluation has started. Call AddAssembly before the first Evaluate().");
        _assemblies.Add(assembly);
        return this;
    }

    public CsEvalEngine AddUsing(string namespaceName)
    {
        if (_frozenConfig != null)
            throw new InvalidOperationException("Cannot add using directives after evaluation has started. Call AddUsing before the first Evaluate().");
        _usingNamespaces.Add(namespaceName);
        return this;
    }

    public CsEvalEngine RegisterExtensionMethods(Type type)
    {
        if (!_extensionTypes.Contains(type))
            _extensionTypes.Insert(0, type);
        return this;
    }

    public CsEvalEngine RegisterExtensionMethods<T>() => RegisterExtensionMethods(typeof(T));

    private IReadOnlyDictionary<string, MemberInfo> BuildMemberDictionary(Type type, bool explicitOnly = false)
    {
        var members = new Dictionary<string, MemberInfo>(_options.StringComparer);

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName)
                continue;

            if (IsAsyncMethod(method))
                continue;

            var attr = method.GetCustomAttribute<CsEvalFunctionAttribute>();

            if (explicitOnly && attr == null)
                continue;

            var name = attr?.Name ?? method.Name;
            members[name] = method;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (!explicitOnly)
                members[prop.Name] = prop;
        }

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (!explicitOnly)
                members[field.Name] = field;
        }

        return members;
    }

    public IReadOnlyDictionary<string, RegisteredModule> GetRegisteredModules()
    {
        var config = GetOrCreateConfig();
        var result = new Dictionary<string, RegisteredModule>(_options.StringComparer);

        foreach (var (name, info) in config.Modules)
        {
            result[name] = new RegisteredModule(info.Type, info.Instance, info.Members);
        }

        return result;
    }

    public sealed record RegisteredModule(Type Type, object? Instance, IReadOnlyDictionary<string, MemberInfo>? Members);

    private void RegisterGlobalFunctions(RegisteredType reg)
    {
        var methods = reg.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<CsEvalFunctionAttribute>();
            if (attr == null) continue;

            var functionName = attr.Name ?? method.Name;
            var moduleInfo = method.IsStatic ? null : new ModuleInfo(reg.Type, reg.Instance, reg.Members);
            _functions[functionName] = CreateFunctionDelegate(method, moduleInfo);
        }
    }

    private static Func<object?[], object?> CreateFunctionDelegate(MethodInfo method, ModuleInfo? moduleInfo)
    {
        return args =>
        {
            var parameters = method.GetParameters();
            var finalArgs = PadWithDefaults(parameters, args);
            return method.Invoke(moduleInfo?.Resolve(null), finalArgs);
        };
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args)
    {
        var result = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            if (i < args.Length)
            {
                result[i] = TypeHelpers.CoerceNumeric(args[i], parameters[i].ParameterType);
            }
            else if (parameters[i].HasDefaultValue)
            {
                result[i] = parameters[i].DefaultValue;
            }
            else
            {
                throw new ArgumentException($"Missing required argument '{parameters[i].Name}'", parameters[i].Name);
            }
        }

        return result;
    }





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
        RegisterModule("Math", typeof(Math));
        RegisterModule("Convert", typeof(Convert));
    }

    private sealed record RegisteredType(
        Type Type,
        object? Instance,
        string? ModuleName,
        IReadOnlyDictionary<string, MemberInfo> Members);
}

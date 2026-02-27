using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using CsEval.Attributes;
using CsEval.Compilation;
using CsEval.Diagnostics;
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
        _typeCache = new TypeCache(_options.ExpressionCompiler);
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

            var parser = ExpressionParser.CreateForSubExpression(tokens, _options.LanguageMode);
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

    public object? Evaluate(
        string expression,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        var parsed = Parse(expression);
        return Evaluate(parsed, variables, serviceProvider, cancellationToken);
    }

    public object? Evaluate(
        CsEvalExpression expression,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        var target = this;
        if (variables != null)
        {
            target = CreateChild();
            target.SetVariables(variables);
        }

        var context = target.GetOrCreateContext(serviceProvider);

        // Initialize or reset constraint state for this evaluation
        var constraints = _options.Constraints;
        if (constraints != null)
        {
            if (context.ConstraintState == null)
            {
                context.ConstraintState = new ExecutionConstraintState();
            }
            context.ConstraintState.Reset(constraints);
        }

        var shouldCompile = _options.CompilationMode is CompilationMode.Compiled or CompilationMode.StrictCompiled;
        if (shouldCompile && expression.GetCompiledInfo() == null)
        {
            expression.TryCompile(_options);
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

    public T? Evaluate<T>(
        string expression,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, serviceProvider, cancellationToken);
        return ConvertResult<T>(result);
    }

    public T? Evaluate<T>(
        CsEvalExpression expression,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, serviceProvider, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates an expression without throwing exceptions.
    /// Returns true if evaluation succeeded, false otherwise.
    /// </summary>
    public bool TryEvaluate(
        string expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            result = Evaluate(expression, variables, serviceProvider, cancellationToken);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Evaluates an expression and converts the result to the specified type without throwing exceptions.
    /// Returns true if evaluation and conversion succeeded, false otherwise.
    /// </summary>
    public bool TryEvaluate<T>(
        string expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            result = Evaluate<T>(expression, variables, serviceProvider, cancellationToken);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Validates an expression for syntax and semantic correctness without evaluating it.
    /// Returns true if the expression is valid, false otherwise.
    /// When false, the diagnostics list contains structured error information.
    /// </summary>
    public bool TryValidate(string expression, out IReadOnlyList<CsEvalDiagnostic> diagnostics)
    {
        Expr ast;
        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();
            var parser = ExpressionParser.CreateForSubExpression(tokens, _options.LanguageMode);
            ast = parser.Parse();
        }
        catch (Exception ex)
        {
            diagnostics = [CsEvalDiagnostic.FromException(ex)];
            return false;
        }

        try
        {
            var context = GetOrCreateContext(null);
            var inferrer = new TypeInferrer(context, _options.MaxExpressionDepth);
            inferrer.InferAll(ast);

            // Check for unbound variables not resolvable in context
            var collector = new VariableCollector();
            collector.Collect(ast);
            var unboundErrors = new List<CsEvalDiagnostic>();
            foreach (var name in collector.Variables)
            {
                if (context.TryGet(name, out _)) continue;
                if (context.Functions.ContainsKey(name)) continue;
                if (context.Modules.ContainsKey(name)) continue;
                if (context.TypeResolver.IsNamespaceOrPrefix(name)) continue;
                if (context.TypeResolver.TryResolveType(name) != null) continue;

                unboundErrors.Add(new CsEvalDiagnostic(
                    DiagnosticSeverity.Error,
                    $"CS0103: The name '{name}' does not exist in the current context",
                    DiagnosticCode.CS0103));
            }

            if (unboundErrors.Count > 0)
            {
                diagnostics = unboundErrors;
                return false;
            }
        }
        catch (Exception ex)
        {
            diagnostics = [CsEvalDiagnostic.FromException(ex)];
            return false;
        }

        diagnostics = [];
        return true;
    }

    private static T? ConvertResult<T>(object? result)
    {
        return result switch
        {
            null => default,
            T typed => typed,
            _ => (T)Convert.ChangeType(result, typeof(T))
        };
    }

    public CsEvalExpression ParseAndCompile(string expression)
    {
        var expr = Parse(expression);
        expr.TryCompile(_options);
        return expr;
    }

    /// <summary>
    /// Compiles an expression and returns a <see cref="CompiledExpression{T}"/> that can be invoked
    /// repeatedly without engine dispatch overhead. Throws if the expression cannot be compiled.
    /// </summary>
    /// <typeparam name="T">The expected return type of the expression.</typeparam>
    /// <param name="expression">The expression string to compile.</param>
    /// <returns>A compiled expression wrapper for repeated invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression cannot be compiled to IL.</exception>
    public CompiledExpression<T> Compile<T>(string expression)
    {
        var parsed = Parse(expression);
        if (!parsed.TryCompile(_options))
        {
            var reason = parsed.CompilationFailureReason ?? "Unknown compilation failure";
            throw new InvalidOperationException(
                $"Cannot compile expression '{expression}': {reason}");
        }

        var compiledDelegate = parsed.GetCompiledInfo()!.Delegate!;
        return new CompiledExpression<T>(compiledDelegate, this, _options, ArgumentTransformer);
    }

    /// <summary>
    /// Compiles an expression and returns a <see cref="CompiledExpression{T}"/> with object? result type.
    /// Throws if the expression cannot be compiled.
    /// </summary>
    /// <param name="expression">The expression string to compile.</param>
    /// <returns>A compiled expression wrapper for repeated invocation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression cannot be compiled to IL.</exception>
    public CompiledExpression<object?> Compile(string expression) => Compile<object?>(expression);

    /// <summary>
    /// Compiles an expression and returns a <see cref="Func{T}"/> for zero-overhead hot-path invocation.
    /// The returned delegate captures the engine context by reference -- variables set via
    /// <see cref="SetVariable"/> after compilation are visible to subsequent invocations.
    /// </summary>
    /// <typeparam name="T">The expected return type of the expression.</typeparam>
    /// <param name="expression">The expression string to compile.</param>
    /// <returns>A function that evaluates the compiled expression and returns the result.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the expression cannot be compiled to IL.</exception>
    public Func<T?> CompileToFunc<T>(string expression)
    {
        var compiled = Compile<T>(expression);
        return () => compiled.Invoke();
    }

    /// <summary>
    /// Parses a lambda expression string into a typed <see cref="Expression{TDelegate}"/>
    /// suitable for Entity Framework, IQueryable providers, and in-memory compilation.
    /// Always parses in Standard mode regardless of engine LanguageMode.
    /// </summary>
    /// <typeparam name="TDelegate">A Func delegate type (e.g., Func&lt;int, bool&gt;).
    /// Parameter types are inferred from the generic arguments.</typeparam>
    /// <param name="expression">A lambda expression string (e.g., "x => x > 5").</param>
    /// <returns>A typed expression tree that can be passed to LINQ providers or compiled.</returns>
    /// <exception cref="CsEvalException">Thrown when the expression contains unsupported nodes
    /// or has parameter/type mismatches.</exception>
    public Expression<TDelegate> ParseAsExpression<TDelegate>(string expression)
        where TDelegate : Delegate
    {
        try
        {
            var delegateType = typeof(TDelegate);
            if (!delegateType.IsGenericType)
                throw new CsEvalException(
                    $"'{delegateType.Name}' is not a generic delegate type. " +
                    $"Use a Func<> type (e.g., Func<int, bool>).");

            var genericArgs = delegateType.GetGenericArguments();
            var paramTypes = genericArgs[..^1];
            var returnType = genericArgs[^1];

            // Always parse in Standard mode regardless of engine LanguageMode
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();
            var parser = ExpressionParser.CreateForSubExpression(tokens, LanguageMode.Standard);
            var ast = parser.Parse();

            // The top-level AST must be a lambda
            if (ast is not LambdaExpr lambdaExpr)
                throw new CsEvalException(
                    "Expression must be a lambda (e.g., 'x => x > 5')");

            // Validate parameter count
            if (lambdaExpr.Parameters.Count != paramTypes.Length)
                throw new CsEvalException(
                    $"Expression has {lambdaExpr.Parameters.Count} parameter(s) " +
                    $"but {delegateType.Name} expects {paramTypes.Length}");

            // Build parameter scope: name -> typed ParameterExpression
            var parameterScope = new Dictionary<string, ParameterExpression>();
            var parameterExpressions = new ParameterExpression[paramTypes.Length];
            for (var i = 0; i < paramTypes.Length; i++)
            {
                var paramName = lambdaExpr.Parameters[i].Name.Lexeme;
                var paramExpr = LinqExpression.Parameter(paramTypes[i], paramName);
                parameterScope[paramName] = paramExpr;
                parameterExpressions[i] = paramExpr;
            }

            // Collect engine variables for ConstantExpression capture
            var engineVariables = CollectEngineVariables();

            // Create emitter and translate the lambda body
            var config = GetOrCreateConfig();
            var emitter = new ExpressionTreeEmitter(parameterScope, engineVariables, config.TypeResolver);
            var body = emitter.Emit(lambdaExpr.Body);

            // If body type doesn't match return type, insert conversion
            if (body.Type != returnType)
            {
                try
                {
                    body = LinqExpression.Convert(body, returnType);
                }
                catch (InvalidOperationException)
                {
                    throw new CsEvalException(
                        $"Cannot convert expression body type '{body.Type.Name}' " +
                        $"to return type '{returnType.Name}'");
                }
            }

            return LinqExpression.Lambda<TDelegate>(body, parameterExpressions);
        }
        catch (InsufficientExecutionStackException)
        {
            throw new CsEvalException("Expression nesting depth exceeded available stack space.");
        }
    }

    /// <summary>
    /// Attempts to parse a lambda expression string into a typed expression tree.
    /// Returns false with diagnostics if parsing or translation fails.
    /// </summary>
    /// <typeparam name="TDelegate">A Func delegate type.</typeparam>
    /// <param name="expression">A lambda expression string.</param>
    /// <param name="result">The resulting expression tree, or null if parsing failed.</param>
    /// <param name="diagnostics">Diagnostic information when parsing fails.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public bool TryParseAsExpression<TDelegate>(
        string expression,
        out Expression<TDelegate>? result,
        out IReadOnlyList<CsEvalDiagnostic> diagnostics)
        where TDelegate : Delegate
    {
        try
        {
            result = ParseAsExpression<TDelegate>(expression);
            diagnostics = [];
            return true;
        }
        catch (Exception ex)
        {
            result = null;
            diagnostics = [CsEvalDiagnostic.FromException(ex)];
            return false;
        }
    }

    private Dictionary<string, object?> CollectEngineVariables()
    {
        var variables = new Dictionary<string, object?>(_options.StringComparer);

        // Collect from pending variables (not yet materialized into context)
        foreach (var (name, value) in _pendingVariables)
        {
            variables[name] = value;
        }

        // Collect from materialized context if it exists
        if (_context != null)
        {
            foreach (var (name, value) in _context.GetAll())
            {
                variables[name] = value;
            }
        }

        return variables;
    }

    /// <summary>
    /// Exposes the engine's evaluation context for use by <see cref="CompiledExpression{T}"/>.
    /// The context is captured by reference so that variable changes after compilation are visible.
    /// </summary>
    internal CsEvalContext GetContextForCompiled() => GetOrCreateContext(null);

    public CsEvalEngine CreateChild()
    {
        var config = GetOrCreateConfig();
        var parentContext = GetOrCreateContext(null);
        return new CsEvalEngine(config, parentContext, _options, _expressionCache);
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
        EnsureNotFrozen();
        _functions[name] = function;
        return this;
    }

    public CsEvalEngine RegisterFromAssembly(Assembly assembly)
    {
        EnsureNotFrozen();
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
        EnsureNotFrozen();
        _registeredTypes.Add(new RegisteredType(type, instance, null, BuildMemberDictionary(type)));
        return this;
    }

    public CsEvalEngine RegisterFromType<T>(T? instance = default) where T : class
    {
        return RegisterFromType(typeof(T), instance);
    }

    public CsEvalEngine RegisterModule(string moduleName, Type type, bool explicitOnly = false, object? instance = null)
    {
        EnsureNotFrozen();
        if (!explicitOnly)
        {
            var moduleAttr = type.GetCustomAttribute<CsEvalModuleAttribute>();
            explicitOnly = moduleAttr?.ExplicitOnly ?? false;
        }
        var methods = BuildMemberDictionary(type, explicitOnly);
        _registeredTypes.Add(new RegisteredType(type, instance, moduleName, methods));
        return this;
    }

    public CsEvalEngine RegisterModule<T>(string moduleName, bool explicitOnly = false, T? instance = default) where T : class
    {
        return RegisterModule(moduleName, typeof(T), explicitOnly, instance);
    }

    public CsEvalEngine RegisterModule(string moduleName, Type type, IReadOnlyDictionary<string, MemberInfo> members)
    {
        EnsureNotFrozen();
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, members));
        return this;
    }

    public CsEvalEngine AddAssembly(Assembly assembly)
    {
        EnsureNotFrozen();
        _assemblies.Add(assembly);
        return this;
    }

    public CsEvalEngine AddUsing(string namespaceName)
    {
        EnsureNotFrozen();
        _usingNamespaces.Add(namespaceName);
        return this;
    }

    public CsEvalEngine RegisterExtensionMethods(Type type)
    {
        EnsureNotFrozen();
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

    private void EnsureNotFrozen([CallerMemberName] string? caller = null)
    {
        if (_frozenConfig != null)
            throw new InvalidOperationException(
                $"Cannot call {caller} after evaluation has started. " +
                $"Call {caller} before the first Evaluate().");
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

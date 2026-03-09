using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CsEval.Attributes;
using CsEval.Binding;
using CsEval.Compilation;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval;

/// <summary>
/// The main entry point for CsEval expression evaluation.
/// </summary>
/// <remarks>
/// <para><b>Thread Safety:</b></para>
/// <para>Configuration methods (RegisterModule, RegisterFunction, RegisterAssembly, RegisterNamespace,
/// SetVariable before first evaluation, etc.) are NOT thread-safe and must be called before the first
/// Evaluate() call. After the first evaluation, the engine configuration is frozen.</para>
/// <para>Evaluate, Parse, Compile, TryValidate, and related methods are thread-safe and can be
/// called concurrently from multiple threads after the engine is frozen.</para>
/// <para>SetVariable is thread-safe during the evaluation phase.</para>
/// <para>Child engines created via CreateChild() can be evaluated concurrently with the parent
/// and with each other.</para>
/// </remarks>
public sealed class CsEvalEngine : IDisposable
{
    private readonly record struct PendingVariable(object? Value, Type InferredType);

    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly List<RegisteredType> _registeredTypes = [];
    private readonly List<Type> _extensionTypes = [];
    private readonly List<Assembly> _assemblies = [];
    private readonly List<string> _usingNamespaces = [];
    private readonly TypeCache _typeCache;
    private readonly ExpressionCache _expressionCache;
    private readonly Dictionary<string, PendingVariable> _pendingVariables;
    private readonly object _contextInitLock = new();

    private CsEvalConfig? _frozenConfig;
    private CsEvalContext? _context;
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _expressionCache.Clear();
        _typeCache.Clear();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public CsEvalEngine() : this(CsEvalOptions.Default)
    {
    }

    public CsEvalEngine(CsEvalOptions options)
    {
        _options = options;
        _typeCache = new TypeCache();
        _expressionCache = new ExpressionCache();
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        _pendingVariables = new Dictionary<string, PendingVariable>(options.StringComparer);
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
        _pendingVariables = new Dictionary<string, PendingVariable>(options.StringComparer);
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

        lock (_contextInitLock)
        {
            if (_context != null)
                return _context;

            var config = GetOrCreateConfig();
            var newContext = new CsEvalContext(config, serviceProvider);

            foreach (var (name, pending) in _pendingVariables)
            {
                newContext.Define(name, pending.Value, pending.InferredType);
            }
            _pendingVariables.Clear();

            _context = newContext;
            return _context;
        }
    }

    public CsEvalExpression Parse(string expression)
    {
        ThrowIfDisposed();
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
        ThrowIfDisposed();
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
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return Evaluate(parsed, variables, serviceProvider, cancellationToken);
    }

    public object? Evaluate(
        CsEvalExpression expression,
        IDictionary<string, object?>? variables = null,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var target = this;
        if (variables != null)
        {
            target = CreateChild();
            target.SetVariables(variables);
        }

        var context = target.GetOrCreateContext(serviceProvider);
        var executionContext = context;

        // Isolate execution constraints per invocation to avoid cross-request
        // interference when the same engine is evaluated concurrently.
        var constraints = _options.Constraints;
        if (constraints != null)
        {
            executionContext = context.CreateChild();
            var state = new ExecutionConstraintState();
            state.Reset(constraints);
            executionContext.ConstraintState = state;
        }

        var shouldCompile = _options.CompilationMode is CompilationMode.Compiled or CompilationMode.StrictCompiled;
        if (shouldCompile)
        {
            if (expression.GetCompiledInfo() == null)
                expression.TryCompile(_options, context);

            var compiled = expression.GetCompiledInfo();
            if (compiled?.Delegate != null)
                return compiled.Delegate(executionContext, _options, cancellationToken);

            if (_options.CompilationMode == CompilationMode.StrictCompiled)
            {
                if (compiled?.FailureException is CsEvalException csEvalFailure)
                    throw csEvalFailure;

                var reason = compiled?.FailureReason ?? "Unknown compilation failure";
                throw new CsEvalException(DiagnosticDescriptors.StrictCompilationFailed, reason);
            }
        }

        if (_options.CompilationMode == CompilationMode.Interpreted)
        {
            if (expression.TryGetOrCreateBoundExpression(executionContext, out var boundExpression, out var boundFailureReason))
            {
                if (boundExpression != null)
                {
                    try
                    {
                        var boundEvaluator = new BoundEvaluator(executionContext, _options, cancellationToken);
                        var boundResult = boundEvaluator.Evaluate(boundExpression);
                        expression.RecordBoundExecution();
                        return boundResult;
                    }
                    catch (BindingNotSupportedException ex)
                    {
                        expression.RecordBoundFallback(ex.Message);
                        // Fall through to existing evaluator until full bound-node coverage is complete.
                    }
                }
                else
                {
                    expression.RecordBoundFallback(boundFailureReason);
                }
            }
            else
            {
                expression.RecordBoundFallback(boundFailureReason);
            }
        }

        var typeInferrer = expression.GetOrCreateTypeInferrer(context, _options.MaxExpressionDepth);
        var evaluator = new Evaluator(executionContext, _options, typeInferrer, cancellationToken);
        return evaluator.Evaluate(expression.Ast);
    }

    public object? Evaluate(
        string expression,
        object variables,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
        => Evaluate(expression, ToVariableDictionary(variables), serviceProvider, cancellationToken);

    public object? Evaluate(
        CsEvalExpression expression,
        object variables,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
        => Evaluate(expression, ToVariableDictionary(variables), serviceProvider, cancellationToken);

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

    public T? Evaluate<T>(
        string expression,
        object variables,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
        => ConvertResult<T>(Evaluate(expression, ToVariableDictionary(variables), serviceProvider, cancellationToken));

    public T? Evaluate<T>(
        CsEvalExpression expression,
        object variables,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
        => ConvertResult<T>(Evaluate(expression, ToVariableDictionary(variables), serviceProvider, cancellationToken));

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
        ThrowIfDisposed();
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
        ThrowIfDisposed();
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
        ThrowIfDisposed();
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

                var ex = new CsEvalException(DiagnosticDescriptors.NameNotInContext, name);
                unboundErrors.Add(CsEvalDiagnostic.FromException(ex));
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
            _ when LambdaDelegateConverter.IsSupportedDelegateType(typeof(T)) =>
                (T)(object)(LambdaDelegateConverter.TryConvert(result, typeof(T))
                    ?? throw new CsEvalException(
                        $"Cannot convert {result.GetType().Name} to delegate type '{typeof(T).Name}'")),
            _ => (T)Convert.ChangeType(result, typeof(T))
        };
    }

    private static Dictionary<string, object?> ToVariableDictionary(object obj)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            dict[prop.Name] = prop.GetValue(obj);
        return dict;
    }

    private Dictionary<string, object?> CollectEngineVariables()
    {
        var variables = new Dictionary<string, object?>(_options.StringComparer);

        // Collect from pending variables (not yet materialized into context)
        lock (_contextInitLock)
        {
            foreach (var (name, pending) in _pendingVariables)
            {
                variables[name] = pending.Value;
            }
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

    internal CompiledFeatureAccess GetCompiledFeatureAccess() => new(this);

    internal sealed class CompiledFeatureAccess
    {
        private readonly CsEvalEngine _engine;

        internal CompiledFeatureAccess(CsEvalEngine engine)
        {
            _engine = engine;
        }

        internal CsEvalOptions Options => _engine._options;
        internal CsEvalConfig GetOrCreateConfig() => _engine.GetOrCreateConfig();
        internal Dictionary<string, object?> CollectEngineVariables() => _engine.CollectEngineVariables();
        internal void ThrowIfDisposed() => _engine.ThrowIfDisposed();
    }

    /// <summary>
    /// Exposes the engine's evaluation context for use by <see cref="CsEvalCompiledExpression{T}"/>.
    /// The context is captured by reference so that variable changes after compilation are visible.
    /// </summary>
    internal CsEvalContext GetContextForCompiled() => GetOrCreateContext(null);

    public CsEvalEngine CreateChild()
    {
        ThrowIfDisposed();
        var config = GetOrCreateConfig();
        var parentContext = GetOrCreateContext(null);
        return new CsEvalEngine(config, parentContext, _options, _expressionCache);
    }

    public CsEvalEngine SetVariable(string name, object? value)
    {
        // Non-generic SetVariable intentionally keeps the compile-time type as object.
        // This preserves C# unboxing semantics for object-typed values while still
        // clearing any previous strongly-typed metadata for the same variable name.
        DefineOrStageVariable(name, value, typeof(object));
        return this;
    }

    /// <summary>
    /// Sets a variable with compile-time type information, enabling optimized expression compilation.
    /// Use this overload when the variable type is known at call time for better performance.
    /// </summary>
    public CsEvalEngine SetVariable<T>(string name, T value)
    {
        DefineOrStageVariable(name, value, typeof(T));
        return this;
    }

    public CsEvalEngine SetVariables(IDictionary<string, object?> variables)
    {
        DefineOrStageVariables(variables, typeof(object));
        return this;
    }

    public CsEvalEngine RegisterFunction(string name, Func<object?[], object?> function)
    {
        EnsureNotFrozen();
        _functions[name] = function;
        return this;
    }

    [RequiresUnreferencedCode("Registering from assembly scans all types and members via reflection.")]
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

            _registeredTypes.Add(new RegisteredType(type, null, null, ModuleMemberMetadata.Build(type, explicitOnly: false, _options.StringComparer)));
        }

        return this;
    }

    public CsEvalEngine RegisterFromType(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        object? instance = null)
    {
        EnsureNotFrozen();
        _registeredTypes.Add(new RegisteredType(type, instance, null, ModuleMemberMetadata.Build(type, explicitOnly: false, _options.StringComparer)));
        return this;
    }

    public CsEvalEngine RegisterFromType<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(T? instance = default) where T : class
    {
        return RegisterFromType(typeof(T), instance);
    }

    public CsEvalEngine RegisterModule(
        string moduleName,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        bool explicitOnly = false,
        object? instance = null)
    {
        EnsureNotFrozen();
        if (!explicitOnly)
        {
            var moduleAttr = type.GetCustomAttribute<CsEvalModuleAttribute>();
            explicitOnly = moduleAttr?.ExplicitOnly ?? false;
        }
        var methods = ModuleMemberMetadata.Build(type, explicitOnly, _options.StringComparer);
        _registeredTypes.Add(new RegisteredType(type, instance, moduleName, methods));
        return this;
    }

    public CsEvalEngine RegisterModule<
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] T>(string moduleName, bool explicitOnly = false, T? instance = default) where T : class
    {
        return RegisterModule(moduleName, typeof(T), explicitOnly, instance);
    }

    public CsEvalEngine RegisterModule(
        string moduleName,
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)] Type type,
        IReadOnlyDictionary<string, MemberInfo> members)
    {
        EnsureNotFrozen();
        _registeredTypes.Add(new RegisteredType(type, null, moduleName, members));
        return this;
    }

    public CsEvalEngine RegisterAssembly(Assembly assembly)
    {
        EnsureNotFrozen();
        _assemblies.Add(assembly);
        return this;
    }

    public CsEvalEngine RegisterNamespace(string namespaceName)
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
            var finalArgs = PadWithDefaults(parameters, args, method.Name);
            return method.Invoke(moduleInfo?.Resolve(null), finalArgs);
        };
    }

    private static object?[] PadWithDefaults(ParameterInfo[] parameters, object?[] args, string callableName)
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
                throw new CsEvalException(
                    DiagnosticDescriptors.MissingRequiredArgument,
                    parameters[i].Name,
                    callableName);
            }
        }

        return result;
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
        var mathMembers = ModuleMemberMetadata.GetBuiltInMathMembers(_options.StringComparer);
        var convertMembers = ModuleMemberMetadata.GetBuiltInConvertMembers(_options.StringComparer);

        _registeredTypes.Add(new RegisteredType(typeof(Math), null, "Math", mathMembers));
        _registeredTypes.Add(new RegisteredType(typeof(Convert), null, "Convert", convertMembers));
    }

    private void DefineOrStageVariable(string name, object? value, Type inferredType)
    {
        if (_context != null)
        {
            _context.Define(name, value, inferredType);
            return;
        }

        lock (_contextInitLock)
        {
            if (_context != null)
                _context.Define(name, value, inferredType);
            else
                _pendingVariables[name] = new PendingVariable(value, inferredType);
        }
    }

    private void DefineOrStageVariables(IDictionary<string, object?> variables, Type inferredType)
    {
        if (_context != null)
        {
            foreach (var (name, value) in variables)
                _context.Define(name, value, inferredType);
            return;
        }

        lock (_contextInitLock)
        {
            if (_context != null)
            {
                foreach (var (name, value) in variables)
                    _context.Define(name, value, inferredType);
            }
            else
            {
                foreach (var (name, value) in variables)
                    _pendingVariables[name] = new PendingVariable(value, inferredType);
            }
        }
    }

    private sealed record RegisteredType(
        [property: DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields)]
        Type Type,
        object? Instance,
        string? ModuleName,
        IReadOnlyDictionary<string, MemberInfo> Members);
}

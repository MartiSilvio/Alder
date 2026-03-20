using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CsEval.Aot;
using CsEval.Attributes;
using CsEval.Binding;
using CsEval.Compilation;
using CsEval.Diagnostics;
using CsEval.Interpretation;
using CsEval.Parsing;
using CsEval.Runtime;
using CsEval.Tracing;

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
public sealed partial class CsEvalEngine : IDisposable
{
    private readonly record struct PendingVariable(object? Value, Type InferredType);

    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly CsEvalOptions _options;
    private readonly List<RegisteredType> _registeredTypes = [];
    private readonly List<Type> _extensionTypes = [];
    private readonly List<Assembly> _assemblies = [];
    private readonly List<string> _usingNamespaces = [];
    private readonly TypeMetadataProvider _typeMetadata;
    private readonly ExpressionCache _expressionCache;
    private readonly Dictionary<string, PendingVariable> _pendingVariables;
    private readonly object _contextInitLock = new();

    private CsEvalTypeContext? _generatedContext;
    private readonly List<CsEvalTypeContext> _additionalContexts = [];
    private CsEvalConfig? _frozenConfig;
    private CsEvalContext? _context;
    private volatile bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _expressionCache.Clear();
        _typeMetadata.Clear();
        _compiledNoCancellationFastPath = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(GetType().FullName);
    }

    public CsEvalEngine() : this(CsEvalOptions.Default)
    {
    }

    public CsEvalEngine(CsEvalOptions options)
    {
        _options = options;
        _typeMetadata = new TypeMetadataProvider();
        _expressionCache = new ExpressionCache();
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        _pendingVariables = new Dictionary<string, PendingVariable>(options.StringComparer);
        _extensionTypes.Add(typeof(Enumerable));
        _generatedContext = Aot.CsEvalBuiltInContext.Default;
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
        _typeMetadata = frozenConfig.TypeMetadata;
        _expressionCache = expressionCache;
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        foreach (var kvp in frozenConfig.Functions) _functions[kvp.Key] = kvp.Value;
        _pendingVariables = new Dictionary<string, PendingVariable>(options.StringComparer);
        _extensionTypes = [..frozenConfig.ExtensionTypes];
        _registeredTypes = [];
    }

    private CsEvalConfig GetOrCreateConfig()
    {
        var config = _frozenConfig;
        if (config != null)
            return config;

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
            [.._assemblies],
            [.._usingNamespaces],
            true,
            _options.StringComparer);

        Dictionary<Type, IAotTypeMetadata>? aotMetadata = null;
        if (_generatedContext != null)
        {
            aotMetadata = new Dictionary<Type, IAotTypeMetadata>();
            foreach (var metadata in _generatedContext.GetTypeMetadata())
                aotMetadata[metadata.Type] = metadata;
        }

        foreach (var ctx in _additionalContexts)
        {
            aotMetadata ??= new Dictionary<Type, IAotTypeMetadata>();
            foreach (var metadata in ctx.GetTypeMetadata())
                aotMetadata[metadata.Type] = metadata;
        }

        var newConfig = CsEvalConfig.Create(_functions, modules, _extensionTypes, _typeMetadata, typeResolver, _options.StringComparer, aotMetadata);
        return Interlocked.CompareExchange(ref _frozenConfig, newConfig, null) ?? newConfig;
    }

    private CsEvalContext GetOrCreateContext(IServiceProvider? serviceProvider)
    {
        var ctx = _context;
        if (ctx != null)
            return ctx;

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
            throw new CsEvalException(DiagnosticDescriptors.ExpressionNestingDepthExceeded);
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
        if (_options.Compiler != null &&
            variables == null &&
            serviceProvider == null &&
            _options.Constraints == null &&
            !cancellationToken.CanBeCanceled)
        {
            if (TryEvaluateCompiledNoCancellationCached(expression, out var cachedResult))
                return cachedResult;

            var fastContext = GetOrCreateContext(null);
            if (TryGetCompiledNoCancellationFastDelegate(expression, fastContext, out var cachedDelegate))
            {
                CacheCompiledNoCancellationFastPath(expression, cachedDelegate, fastContext);
                try
                {
                    return cachedDelegate(fastContext);
                }
                catch (CsEvalException ex) when (ex.Span.IsEmpty && !expression.Ast.Span.IsEmpty)
                {
                    EnrichCompiledExceptionDiagnostics(ex, expression);
                    throw;
                }
            }

            return ExecuteCompiledExpression(
                expression,
                fastContext,
                fastContext,
                cancellationToken,
                allowNoCancellationFastPath: true);
        }

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

        if (_options.Compiler != null)
        {
            var allowNoCancellationFastPath = constraints == null && !cancellationToken.CanBeCanceled;
            return ExecuteCompiledExpression(
                expression,
                context,
                executionContext,
                cancellationToken,
                allowNoCancellationFastPath);
        }

        if (expression.TryGetOrCreateBoundExpression(executionContext, _options.MaxExpressionDepth, out var boundExpression, out var boundFailureReason))
        {
            if (boundExpression != null)
            {
                try
                {
                    var boundEvaluator = new BoundEvaluator(executionContext, _options, cancellationToken, sourceText: new Text.SourceText(expression.Source));
                    var boundResult = boundEvaluator.Evaluate(boundExpression);
                    expression.RecordBoundExecution();
                    return UnwrapControlFlowSignal(boundResult);
                }
                catch (BindingNotSupportedException ex)
                {
                    expression.RecordBoundFallback(ex.Message);
                    if (IsDepthFailure(ex.Message))
                        throw new CsEvalDepthException("binding", _options.MaxExpressionDepth);
                    throw new CsEvalException(DiagnosticDescriptors.BindingFailed, ex.Message);
                }
            }
        }

        expression.RecordBoundFallback(boundFailureReason);
        if (IsDepthFailure(boundFailureReason))
            throw new CsEvalDepthException("binding", _options.MaxExpressionDepth);
        throw new CsEvalException(DiagnosticDescriptors.BindingFailed, boundFailureReason ?? "Binding failed for expression.");
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
            AstDepthValidator.EnsureWithinLimit(ast, _options.MaxExpressionDepth);
            var binder = new CsEval.Binding.Binder(new Text.SourceText(expression), recovering: true);
            var bindingContext = new BindingContext(context);
            var validationDiagnostics = new List<CsEvalDiagnostic>(binder.CollectDiagnostics(ast, bindingContext));

            var collector = new IdentifierOccurrenceCollector();
            collector.Collect(ast);
            foreach (var identifier in collector.GetUnboundTokens(_options.StringComparer))
            {
                var name = identifier.Lexeme;
                if (context.TryGet(name, out _)) continue;
                if (context.Functions.ContainsKey(name)) continue;
                if (context.Modules.ContainsKey(name)) continue;
                if (context.TypeResolver.IsNamespaceOrPrefix(name)) continue;
                if (context.TypeResolver.TryResolveType(name) != null) continue;

                var message = $"{DiagnosticDescriptors.NameNotInContext.Code.ToDiagnosticId()}: {DiagnosticDescriptors.NameNotInContext.FormatMessage(name)}";
                validationDiagnostics.Add(new CsEvalDiagnostic(
                    DiagnosticSeverity.Error, message, DiagnosticDescriptors.NameNotInContext.Code,
                    identifier.Span, identifier.Line, identifier.Column));
            }

            var deduplicated = DeduplicateDiagnostics(validationDiagnostics);
            if (deduplicated.Count > 0)
            {
                diagnostics = deduplicated;
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

    private static IReadOnlyList<CsEvalDiagnostic> DeduplicateDiagnostics(List<CsEvalDiagnostic> diagnostics)
    {
        if (diagnostics.Count <= 1)
            return diagnostics;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<CsEvalDiagnostic>(diagnostics.Count);
        foreach (var diagnostic in diagnostics)
        {
            var key = $"{diagnostic.Code}|{diagnostic.Span}|{diagnostic.Message}";
            if (!seen.Add(key))
                continue;
            result.Add(diagnostic);
        }

        return result;
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
                        DiagnosticDescriptors.DelegateConversionFailed, result.GetType().Name, typeof(T).Name)),
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

    private static object? UnwrapControlFlowSignal(object? result) =>
        result is ControlFlowSignal signal ? signal.Value : result;

    private static bool IsDepthFailure(string? message)
    {
        return !string.IsNullOrEmpty(message) &&
               message.IndexOf("nesting depth exceeded", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private Dictionary<string, object?> CollectEngineVariables()
    {
        var variables = new Dictionary<string, object?>(_options.StringComparer);

        lock (_contextInitLock)
        {
            foreach (var (name, pending) in _pendingVariables)
            {
                variables[name] = pending.Value;
            }
        }

        if (_context != null)
        {
            foreach (var (name, value) in _context.GetAll())
            {
                variables[name] = value;
            }
        }

        return variables;
    }

    public CsEvalEngine CreateChild()
    {
        ThrowIfDisposed();
        var config = GetOrCreateConfig();
        var parentContext = GetOrCreateContext(null);
        return new CsEvalEngine(config, parentContext, _options, _expressionCache);
    }
}

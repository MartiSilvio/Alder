using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Alder.Aot;
using Alder.Attributes;
using Alder.Binding;
using Alder.Compilation;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Tracing;
using Binder = Alder.Binding.Binder;

namespace Alder;

/// <summary>
/// The main entry point for Alder expression evaluation.
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
public sealed partial class AlderEngine : IDisposable
{
    private readonly record struct PendingVariable(object? Value, Type InferredType);

    private readonly Dictionary<string, Func<object?[], object?>> _functions;
    private readonly AlderOptions _options;
    private readonly List<RegisteredType> _registeredTypes = [];
    private readonly List<Type> _extensionTypes = [];
    private readonly List<Assembly> _assemblies = [];
    private readonly List<string> _usingNamespaces = [];
    private readonly TypeMetadataProvider _typeMetadata;
    private readonly ExpressionCache _expressionCache;
    private readonly Dictionary<string, PendingVariable> _pendingVariables;
    private readonly object _contextInitLock = new();

    private AlderTypeContext? _generatedContext;
    private readonly List<AlderTypeContext> _additionalContexts = [];
    private AlderConfig? _frozenConfig;
    private AlderContext? _context;
    private readonly DisposalToken _disposalToken;

    public void Dispose()
    {
        if (_disposalToken.IsDisposed) return;
        _disposalToken.IsDisposed = true;
        _expressionCache.Clear();
        _typeMetadata.Clear();
        _compiledNoCancellationFastPath = null;
    }

    private void ThrowIfDisposed()
    {
        if (_disposalToken.IsDisposed) throw new ObjectDisposedException(GetType().FullName);
    }

    private sealed class DisposalToken
    {
        public volatile bool IsDisposed;
    }

    public AlderEngine() : this(AlderOptions.Default)
    {
    }

    public AlderEngine(AlderOptions options)
    {
        _options = options;
        _disposalToken = new DisposalToken();
        _typeMetadata = new TypeMetadataProvider();
        _expressionCache = new ExpressionCache();
        _functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        _pendingVariables = new Dictionary<string, PendingVariable>(options.StringComparer);
        _extensionTypes.Add(typeof(Enumerable));
        _generatedContext = Aot.AlderBuiltInContext.Default;
        RegisterBuiltInModules();
    }

    private AlderEngine(
        AlderConfig frozenConfig,
        AlderContext parentContext,
        AlderOptions options,
        ExpressionCache expressionCache,
        DisposalToken disposalToken)
    {
        _frozenConfig = frozenConfig;
        _disposalToken = disposalToken;
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

    private static readonly Pipeline.BoundTreePipeline SecurityOnlyPipeline =
        Pipeline.BoundTreePipeline.Create(Security.SecurityValidationPass.Instance);

    private Pipeline.BoundTreePipeline? _compilationPipeline;
    private Pipeline.BoundTreePipeline GetOrCreateCompilationPipeline()
    {
        return _compilationPipeline ??= Pipeline.BoundTreePipeline.Create(
            Security.SecurityValidationPass.Instance,
            new Binding.Optimization.ConstantFoldingPass(),
            new Binding.Optimization.DeadBranchEliminationPass(),
            new Binding.Optimization.ConversionInsertionPass());
    }

    private Binding.BoundExpr RunPipeline(Binding.BoundExpr tree, CancellationToken ct = default)
    {
        var context = new Pipeline.PipelineContext(_options.Security, _options, ct);
        return SecurityOnlyPipeline.Execute(tree, context);
    }

    private Binding.BoundExpr RunCompilationPipeline(Binding.BoundExpr tree, CancellationToken ct = default)
    {
        var context = new Pipeline.PipelineContext(_options.Security, _options, ct);
        return GetOrCreateCompilationPipeline().Execute(tree, context);
    }

    private AlderConfig GetOrCreateConfig()
    {
        var config = _frozenConfig;
        if (config != null)
            return config;

        var modules = new Dictionary<string, ModuleInfo>(_options.StringComparer);
        foreach (var reg in _registeredTypes)
        {
            var moduleName = reg.ModuleName ?? reg.Type.GetCustomAttribute<AlderModuleAttribute>()?.Name;
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

        var newConfig = AlderConfig.Create(_functions, modules, _extensionTypes, _typeMetadata, typeResolver, _options.StringComparer, aotMetadata);
        return Interlocked.CompareExchange(ref _frozenConfig, newConfig, null) ?? newConfig;
    }

    private AlderContext GetOrCreateContext(IServiceProvider? serviceProvider)
    {
        var ctx = _context;
        if (ctx != null)
            return ctx;

        lock (_contextInitLock)
        {
            if (_context != null)
                return _context;

            var config = GetOrCreateConfig();
            var newContext = new AlderContext(config, serviceProvider);

            foreach (var (name, pending) in _pendingVariables)
            {
                newContext.Define(name, pending.Value, pending.InferredType);
            }
            _pendingVariables.Clear();

            _context = newContext;
            return _context;
        }
    }

    public AlderExpression Parse(string expression)
    {
        ThrowIfDisposed();
        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();

            var parser = ExpressionParser.CreateForSubExpression(tokens, _options.LanguageMode);
            var ast = parser.Parse();

            return new AlderExpression(expression, ast, _expressionCache);
        }
        catch (System.InsufficientExecutionStackException)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded);
        }
    }

    public bool TryParse(string expression, out AlderExpression? result, out string? error)
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

    public bool TryParse(string expression, out AlderExpression? result)
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
        AlderExpression expression,
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
                catch (AlderException ex) when (ex.Span.IsEmpty && !expression.Ast.Span.IsEmpty)
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
                    boundExpression = RunPipeline(boundExpression, cancellationToken);
                    var boundEvaluator = new BoundEvaluator(executionContext, _options, cancellationToken, sourceText: new Text.SourceText(expression.Source));
                    var boundResult = boundEvaluator.Evaluate(boundExpression);
                    expression.RecordBoundExecution();
                    return UnwrapControlFlowSignal(boundResult);
                }
                catch (BindingNotSupportedException ex)
                {
                    expression.RecordBoundFallback(ex.Message);
                    if (IsDepthFailure(ex.Message))
                        throw new AlderDepthException("binding", _options.MaxExpressionDepth);
                    throw new AlderException(DiagnosticDescriptors.BindingFailed, ex.Message);
                }
            }
        }

        expression.RecordBoundFallback(boundFailureReason);
        if (IsDepthFailure(boundFailureReason))
            throw new AlderDepthException("binding", _options.MaxExpressionDepth);
        throw new AlderException(DiagnosticDescriptors.BindingFailed, boundFailureReason ?? "Binding failed for expression.");
    }

    public object? Evaluate(
        string expression,
        object variables,
        IServiceProvider? serviceProvider = null,
        CancellationToken cancellationToken = default)
        => Evaluate(expression, ToVariableDictionary(variables), serviceProvider, cancellationToken);

    public object? Evaluate(
        AlderExpression expression,
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
        AlderExpression expression,
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
        AlderExpression expression,
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
    public bool TryValidate(string expression, out IReadOnlyList<AlderDiagnostic> diagnostics)
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
            diagnostics = [AlderDiagnostic.FromException(ex)];
            return false;
        }

        try
        {
            var context = GetOrCreateContext(null);
            AstDepthValidator.EnsureWithinLimit(ast, _options.MaxExpressionDepth);
            var binder = new Binder(new Text.SourceText(expression), recovering: true);
            var bindingContext = new BindingContext(context);
            var validationDiagnostics = new List<AlderDiagnostic>(binder.CollectDiagnostics(ast, bindingContext));

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
                validationDiagnostics.Add(new AlderDiagnostic(
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
            diagnostics = [AlderDiagnostic.FromException(ex)];
            return false;
        }

        diagnostics = [];
        return true;
    }

    private static IReadOnlyList<AlderDiagnostic> DeduplicateDiagnostics(List<AlderDiagnostic> diagnostics)
    {
        if (diagnostics.Count <= 1)
            return diagnostics;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<AlderDiagnostic>(diagnostics.Count);
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
                    ?? throw new AlderException(
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

    public AlderEngine CreateChild()
    {
        ThrowIfDisposed();
        var config = GetOrCreateConfig();
        var parentContext = GetOrCreateContext(null);
        return new AlderEngine(config, parentContext, _options, _expressionCache, _disposalToken);
    }
}

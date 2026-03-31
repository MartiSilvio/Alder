using System.Collections.Concurrent;
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
using Binder = Alder.Binding.Binder;

namespace Alder;

/// <summary>
/// The main entry point for Alder expression evaluation.
/// Provides methods for parsing, evaluating, validating, and compiling C# expressions at runtime.
/// </summary>
/// <remarks>
/// <para><b>Thread Safety:</b></para>
/// <para>The engine is fully configured at construction time via <see cref="AlderOptions"/>.
/// All evaluation methods are thread-safe and can be called concurrently from multiple threads.</para>
/// <para><see cref="SetVariable{T}"/> is thread-safe and can be called at any time, including between evaluations.</para>
/// <para>Child engines created via <see cref="CreateChild"/> can be evaluated concurrently with the parent
/// and with each other.</para>
/// </remarks>
public sealed partial class AlderEngine : IDisposable
{
    private readonly record struct PendingVariable(object? Value, Type InferredType);

    private readonly AlderConfig _config;
    private readonly TypeMetadataProvider _typeMetadata;
    private readonly ExpressionCache _expressionCache;
    private readonly Dictionary<string, PendingVariable> _pendingVariables;
    private readonly object _contextInitLock = new();

    private AlderContext? _context;
    private readonly DisposalToken _disposalToken;
    private readonly ConditionalWeakTable<Binding.BoundExpr, StrongBox<Binding.BoundExpr>> _pipelineCache = new();

    private static readonly ConcurrentDictionary<Type, (string Name, Func<object, object?> Getter)[]> VariableAccessorCache = new();
    private static readonly MethodInfo? WrapGetterMethod =
        typeof(AlderEngine).GetMethod(nameof(WrapGetter), BindingFlags.NonPublic | BindingFlags.Static);

    public void Dispose()
    {
        if (_disposalToken.IsDisposed) return;
        _disposalToken.IsDisposed = true;
        _expressionCache.Clear();
        _typeMetadata.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (_disposalToken.IsDisposed) throw new ObjectDisposedException(GetType().FullName);
    }

    private sealed class DisposalToken
    {
        public volatile bool IsDisposed;
    }

    public AlderEngine() : this(new AlderOptions())
    {
    }

    /// <summary>
    /// Creates a new engine configured via the provided options action.
    /// </summary>
    /// <param name="configure">An action that configures the engine options.</param>
    public AlderEngine(Action<AlderOptions> configure) : this(Apply(configure))
    {
    }

    /// <summary>
    /// Creates a new engine with the specified options.
    /// </summary>
    /// <param name="options">The configuration options for this engine.</param>
    public AlderEngine(AlderOptions options)
    {
        _disposalToken = new DisposalToken();
        _typeMetadata = new TypeMetadataProvider();
        _expressionCache = new ExpressionCache();
        _config = BuildConfig(options);
        _pendingVariables = new Dictionary<string, PendingVariable>(_config.Comparer);
    }

    internal bool HasCompiler => _config.Compiler != null;

    private AlderEngine(
        AlderConfig config,
        AlderContext parentContext,
        ExpressionCache expressionCache,
        DisposalToken disposalToken)
    {
        _config = config;
        _disposalToken = disposalToken;
        _context = parentContext.CreateChild();
        _typeMetadata = config.TypeMetadata;
        _expressionCache = expressionCache;
        _pendingVariables = new Dictionary<string, PendingVariable>(config.Comparer);
    }

    private static AlderOptions Apply(Action<AlderOptions> configure)
    {
        var options = new AlderOptions();
        configure(options);
        return options;
    }

    private static AlderConfig BuildConfig(AlderOptions options)
    {
        var functions = new Dictionary<string, Func<object?[], object?>>(options.StringComparer);
        foreach (var kvp in options.Functions.RegisteredFunctions)
            functions[kvp.Key] = kvp.Value;

        var modules = new Dictionary<string, ModuleInfo>(options.StringComparer);

        var mathMembers = ModuleMemberMetadata.GetBuiltInMathMembers(options.StringComparer);
        var convertMembers = ModuleMemberMetadata.GetBuiltInConvertMembers(options.StringComparer);
        modules["Math"] = new ModuleInfo(typeof(Math), null, mathMembers);
        modules["Convert"] = new ModuleInfo(typeof(Convert), null, convertMembers);

        foreach (var reg in options.Modules.RegisteredTypes)
        {
            var moduleName = reg.ModuleName ?? reg.Type.GetCustomAttribute<AlderModuleAttribute>()?.Name;
            if (moduleName != null)
            {
                modules[moduleName] = new ModuleInfo(reg.Type, reg.Instance, reg.Members);
            }
            else
            {
                RegisterGlobalFunctions(reg, functions);
            }
        }

        var typeResolver = TypeResolver.Create(
            [..options.Types.Assemblies],
            [..options.Types.Namespaces],
            true,
            options.StringComparer);

        Dictionary<Type, ITypedDispatch>? typeDispatch = null;
        Dictionary<Type, Func<object, Delegate>>? delegateFactories = null;

        if (options.Aot.BuiltInContext != null)
        {
            typeDispatch = new Dictionary<Type, ITypedDispatch>();
            foreach (var metadata in options.Aot.BuiltInContext.GetTypeMetadata())
                typeDispatch[metadata.Type] = metadata;

            var factories = options.Aot.BuiltInContext.GetDelegateFactories();
            if (factories != null)
            {
                delegateFactories = new Dictionary<Type, Func<object, Delegate>>();
                foreach (var kvp in factories)
                    delegateFactories[kvp.Key] = kvp.Value;
            }
        }

        foreach (var ctx in options.Aot.AdditionalContexts)
        {
            typeDispatch ??= new Dictionary<Type, ITypedDispatch>();
            foreach (var metadata in ctx.GetTypeMetadata())
                typeDispatch[metadata.Type] = metadata;

            var factories = ctx.GetDelegateFactories();
            if (factories != null)
            {
                delegateFactories ??= new Dictionary<Type, Func<object, Delegate>>();
                foreach (var kvp in factories)
                    delegateFactories[kvp.Key] = kvp.Value;
            }
        }

        LambdaDelegateConverter.SetAotFactories(delegateFactories);

        var typeMetadata = new TypeMetadataProvider();
        return new AlderConfig(
            options.LanguageMode,
            options.Sandbox.ToSecurityPolicy(),
            options.IsCaseSensitive,
            options.Constraints,
            options.Compiler,
            options.ExpressionCompiler,
            options.ServiceProvider,
            Runtime.Collections.FixedDictionary<string, Func<object?[], object?>>.Create(functions),
            Runtime.Collections.FixedDictionary<string, ModuleInfo>.Create(modules),
            [..options.Types.ExtensionTypes],
            typeMetadata,
            typeResolver,
            typeDispatch != null ? Runtime.Collections.FixedDictionary<Type, ITypedDispatch>.Create(typeDispatch) : null,
            delegateFactories);
    }

    private static void RegisterGlobalFunctions(AlderOptions.RegisteredType reg, Dictionary<string, Func<object?[], object?>> functions)
    {
        var methods = reg.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<AlderFunctionAttribute>();
            if (attr == null) continue;

            var functionName = attr.Name ?? method.Name;
            var moduleInfo = method.IsStatic ? null : new ModuleInfo(reg.Type, reg.Instance, reg.Members);
            functions[functionName] = CreateFunctionDelegate(method, moduleInfo);
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
                throw new AlderException(
                    DiagnosticDescriptors.MissingRequiredArgument,
                    parameters[i].Name,
                    callableName);
            }
        }

        return result;
    }

    private static readonly Pipeline.BoundTreePipeline SecurityOnlyPipeline =
        Pipeline.BoundTreePipeline.Create(Security.SecurityValidationPass.Instance);

    private static readonly Pipeline.BoundTreePipeline InterpretationPipeline =
        Pipeline.BoundTreePipeline.Create(
            Security.SecurityValidationPass.Instance,
            new Binding.Optimization.ConstantFoldingPass(),
            new Binding.Optimization.DeadBranchEliminationPass());

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
        var context = new Pipeline.PipelineContext(_config.Security, ct);
        return InterpretationPipeline.Execute(tree, context);
    }

    private Binding.BoundExpr RunSecurityOnlyPipeline(Binding.BoundExpr tree, CancellationToken ct = default)
    {
        var context = new Pipeline.PipelineContext(_config.Security, ct);
        return SecurityOnlyPipeline.Execute(tree, context);
    }

    private Binding.BoundExpr RunCompilationPipeline(Binding.BoundExpr tree, CancellationToken ct = default)
    {
        var context = new Pipeline.PipelineContext(_config.Security, ct);
        return GetOrCreateCompilationPipeline().Execute(tree, context);
    }

    private AlderContext GetOrCreateContext()
    {
        var ctx = _context;
        if (ctx != null)
            return ctx;

        lock (_contextInitLock)
        {
            if (_context != null)
                return _context;

            var newContext = new AlderContext(_config, _config.ServiceProvider);

            foreach (var (name, pending) in _pendingVariables)
            {
                newContext.Define(name, pending.Value, pending.InferredType);
            }
            _pendingVariables.Clear();

            _context = newContext;
            return _context;
        }
    }

    /// <summary>
    /// Parses an expression string into a reusable <see cref="AlderExpression"/> that can be evaluated multiple times.
    /// </summary>
    /// <param name="expression">The C# expression string to parse.</param>
    /// <returns>A parsed expression ready for evaluation.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">The expression contains syntax errors.</exception>
    public AlderExpression Parse(string expression)
    {
        ThrowIfDisposed();
        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();

            var parser = ExpressionParser.CreateForSubExpression(tokens, _config.LanguageMode);
            var ast = parser.Parse();

            return new AlderExpression(expression, ast, _expressionCache);
        }
        catch (System.InsufficientExecutionStackException)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded);
        }
    }

    /// <summary>
    /// Attempts to parse an expression string without throwing on failure.
    /// </summary>
    /// <param name="expression">The C# expression string to parse.</param>
    /// <param name="result">When successful, the parsed expression; otherwise, <c>null</c>.</param>
    /// <param name="error">When parsing fails, the error message; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
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

    /// <inheritdoc cref="TryParse(string, out AlderExpression?, out string?)"/>
    public bool TryParse(string expression, out AlderExpression? result)
    {
        return TryParse(expression, out result, out _);
    }

    /// <summary>
    /// Evaluates a C# expression string and returns the result.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">The expression contains errors or evaluation fails.</exception>
    public object? Evaluate(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var parsed = Parse(expression);
        return Evaluate(parsed, variables, cancellationToken);
    }

    /// <summary>
    /// Evaluates a pre-parsed expression and returns the result.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    /// <exception cref="AlderException">Evaluation fails due to binding or runtime errors.</exception>
    public object? Evaluate(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var target = this;
        if (variables != null)
        {
            target = CreateChild();
            target.SetVariables(variables);
        }

        var context = target.GetOrCreateContext();
        var constraintState = new ExecutionConstraintState();
        constraintState.Reset(_config.Constraints);

        if (_config.Compiler != null)
        {
            return ExecuteCompiledExpression(
                expression,
                context,
                context,
                constraintState,
                cancellationToken);
        }

        var executionContext = context.CreateChild();

        try
        {
            return EvaluateCore(expression, context, executionContext, constraintState, cancellationToken);
        }
        catch (System.InsufficientExecutionStackException)
        {
            throw new AlderException(DiagnosticDescriptors.ExpressionNestingDepthExceeded);
        }
    }

    private object? EvaluateCore(
        AlderExpression expression,
        AlderContext bindingContext,
        AlderContext executionContext,
        ExecutionConstraintState constraintState,
        CancellationToken cancellationToken)
    {
        if (expression.TryGetOrCreateBoundExpression(bindingContext, out var boundExpression, out var boundFailureReason))
        {
            if (boundExpression != null)
            {
                try
                {
                    var processed = _pipelineCache.GetValue(boundExpression,
                        b => new StrongBox<Binding.BoundExpr>(RunPipeline(b, cancellationToken)));

                    var boundEvaluator = new BoundEvaluator(executionContext, _config, constraintState, sourceText: new Text.SourceText(expression.Source), cancellationToken: cancellationToken);
                    var boundResult = boundEvaluator.Evaluate(processed.Value!);
                    expression.RecordBoundExecution();
                    return UnwrapControlFlowSignal(boundResult);
                }
                catch (BindingNotSupportedException ex)
                {
                    expression.RecordBoundFallback(ex.Message);
                    throw new AlderException(DiagnosticDescriptors.BindingFailed, ex.Message);
                }
            }
        }

        expression.RecordBoundFallback(boundFailureReason);
        throw new AlderException(DiagnosticDescriptors.BindingFailed, boundFailureReason ?? "Binding failed for expression.");
    }

    /// <summary>
    /// Evaluates a C# expression with variables supplied as an anonymous object.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public object? Evaluate(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => Evaluate(expression, ToVariableDictionary(variables), cancellationToken);

    /// <summary>
    /// Evaluates a pre-parsed expression with variables supplied as an anonymous object.
    /// </summary>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result of evaluating the expression, or <c>null</c>.</returns>
    public object? Evaluate(
        AlderExpression expression,
        object variables,
        CancellationToken cancellationToken = default)
        => Evaluate(expression, ToVariableDictionary(variables), cancellationToken);

    /// <summary>
    /// Evaluates a C# expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        string expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates a pre-parsed expression and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        AlderExpression expression,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        var result = Evaluate(expression, variables, cancellationToken);
        return ConvertResult<T>(result);
    }

    /// <summary>
    /// Evaluates a C# expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        string expression,
        object variables,
        CancellationToken cancellationToken = default)
        => ConvertResult<T>(Evaluate(expression, ToVariableDictionary(variables), cancellationToken));

    /// <summary>
    /// Evaluates a pre-parsed expression with anonymous object variables and converts the result to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The pre-parsed expression to evaluate.</param>
    /// <param name="variables">An object whose public properties become expression variables.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns>The result converted to <typeparamref name="T"/>, or <c>default</c> if the result is <c>null</c>.</returns>
    public T? Evaluate<T>(
        AlderExpression expression,
        object variables,
        CancellationToken cancellationToken = default)
        => ConvertResult<T>(Evaluate(expression, ToVariableDictionary(variables), cancellationToken));

    /// <summary>
    /// Attempts to evaluate a C# expression without throwing on failure.
    /// </summary>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="result">When successful, the evaluation result; otherwise, <c>null</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation succeeded; otherwise, <c>false</c>.</returns>
    public bool TryEvaluate(
        string expression,
        out object? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            result = Evaluate(expression, variables, cancellationToken);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to evaluate a C# expression and convert the result to <typeparamref name="T"/> without throwing on failure.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="expression">The C# expression string to evaluate.</param>
    /// <param name="result">When successful, the result converted to <typeparamref name="T"/>; otherwise, <c>default</c>.</param>
    /// <param name="variables">Optional variables accessible within the expression.</param>
    /// <param name="cancellationToken">Token to cancel evaluation.</param>
    /// <returns><c>true</c> if evaluation and conversion succeeded; otherwise, <c>false</c>.</returns>
    public bool TryEvaluate<T>(
        string expression,
        out T? result,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        try
        {
            result = Evaluate<T>(expression, variables, cancellationToken);
            return true;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Validates an expression for syntax and binding errors without evaluating it.
    /// </summary>
    /// <param name="expression">The C# expression string to validate.</param>
    /// <param name="diagnostics">When validation fails, the list of diagnostics; otherwise, an empty list.</param>
    /// <returns><c>true</c> if the expression is valid; otherwise, <c>false</c>.</returns>
    public bool TryValidate(string expression, out IReadOnlyList<AlderDiagnostic> diagnostics)
    {
        ThrowIfDisposed();
        Expr ast;
        try
        {
            var lexer = new Lexer(expression);
            var tokens = lexer.Tokenize();
            var parser = ExpressionParser.CreateForSubExpression(tokens, _config.LanguageMode);
            ast = parser.Parse();
        }
        catch (Exception ex)
        {
            diagnostics = [AlderDiagnostic.FromException(ex)];
            return false;
        }

        try
        {
            var context = GetOrCreateContext();
            var binder = new Binder(new Text.SourceText(expression));
            var bindingContext = new BindingContext(context);
            var validationDiagnostics = new List<AlderDiagnostic>(binder.CollectDiagnostics(ast, bindingContext));

            var collector = new IdentifierOccurrenceCollector();
            collector.Collect(ast);
            foreach (var identifier in collector.GetUnboundTokens(_config.Comparer))
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
        var accessors = VariableAccessorCache.GetOrAdd(obj.GetType(), static t =>
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var result = new (string Name, Func<object, object?> Getter)[props.Length];
            for (var i = 0; i < props.Length; i++)
            {
                var prop = props[i];
                var getter = prop.GetGetMethod();
                if (getter == null || WrapGetterMethod == null)
                {
                    var p = prop;
                    result[i] = (prop.Name, o => p.GetValue(o));
                    continue;
                }

                var boxed = (Func<object, object?>)WrapGetterMethod
                    .MakeGenericMethod(t, prop.PropertyType)
                    .Invoke(null, [getter])!;
                result[i] = (prop.Name, boxed);
            }
            return result;
        });

        var dict = new Dictionary<string, object?>(accessors.Length);
        foreach (var (name, getter) in accessors)
            dict[name] = getter(obj);
        return dict;
    }

    private static Func<object, object?> WrapGetter<TOwner, TProp>(MethodInfo getMethod)
    {
        var typed = (Func<TOwner, TProp>)Delegate.CreateDelegate(typeof(Func<TOwner, TProp>), getMethod);
        return obj => typed((TOwner)obj);
    }

    private static object? UnwrapControlFlowSignal(object? result) =>
        result is ControlFlowSignal signal ? signal.Value : result;

    private Dictionary<string, object?> CollectEngineVariables()
    {
        var variables = new Dictionary<string, object?>(_config.Comparer);

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

    /// <summary>
    /// Creates a child engine that inherits this engine's configuration and variables.
    /// The child can define additional variables without affecting the parent.
    /// </summary>
    /// <returns>A new child engine.</returns>
    /// <exception cref="ObjectDisposedException">The engine has been disposed.</exception>
    public AlderEngine CreateChild()
    {
        ThrowIfDisposed();
        var parentContext = GetOrCreateContext();
        return new AlderEngine(_config, parentContext, _expressionCache, _disposalToken);
    }
}

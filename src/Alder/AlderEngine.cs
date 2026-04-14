using System.Runtime.CompilerServices;
using Alder.Aot;
using Alder.Attributes;
using Alder.Binding;
using Alder.Compilation;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Runtime;

namespace Alder;

/// <summary>
/// The primary runtime entry point for parsing, validating, evaluating, and compiling Alder expressions.
/// </summary>
/// <remarks>
/// <para>The engine is configured once at construction time.</para>
/// <para>Concurrent evaluation is supported on the root engine and on child engines created by <see cref="CreateChild"/>.</para>
/// <para>Concurrent mutation of shared parent-scoped variables is not a synchronization contract.
/// Compound updates such as <c>x = x + 1</c> are not atomic, and evaluation does not provide snapshot isolation
/// against concurrent writes. Use child-local variables or external synchronization when shared state is mutated.</para>
/// </remarks>
public sealed partial class AlderEngine : IDisposable
{
    private readonly record struct PendingVariable(object? Value, Type InferredType);

    private readonly AlderConfig _config;
    private readonly TypeMetadataProvider _typeMetadata;
    private readonly ExpressionCache _expressionCache;
    private readonly Dictionary<string, PendingVariable> _pendingVariables;
    private readonly object _contextInitLock = new();
    private readonly AlderEngine? _parentEngine;

    private AlderContext? _context;
    private readonly DisposalToken _disposalToken;
    private readonly ConditionalWeakTable<BoundExpr, BoundExpr> _pipelineCache = new();

    public void Dispose()
    {
        if (_disposalToken.IsDisposed) return;
        _disposalToken.IsDisposed = true;

        if (_parentEngine == null)
        {
            _expressionCache.Clear();
            _typeMetadata.Clear();
        }
    }

    private void ThrowIfDisposed()
    {
        if (IsDisposed())
            throw new ObjectDisposedException(GetType().FullName);
    }

    private sealed class DisposalToken
    {
        public volatile bool IsDisposed;
    }

    public AlderEngine() : this(new AlderOptions())
    {
    }

    /// <summary>
    /// Creates a new engine configured through an options callback.
    /// </summary>
    /// <param name="configure">An action that configures the engine options.</param>
    public AlderEngine(Action<AlderOptions> configure) : this(Apply(configure))
    {
    }

    /// <summary>
    /// Creates a new engine with the supplied options.
    /// </summary>
    /// <param name="options">The configuration options for this engine.</param>
    public AlderEngine(AlderOptions options)
    {
        _parentEngine = null;
        _disposalToken = new DisposalToken();
        _typeMetadata = new TypeMetadataProvider();
        _expressionCache = new ExpressionCache();
        _config = BuildConfig(options);
        _pendingVariables = new Dictionary<string, PendingVariable>(_config.Comparer);
    }

    internal bool HasCompiler => _config.Compiler != null;

    private AlderEngine(
        AlderEngine parentEngine,
        AlderConfig config,
        AlderContext parentContext,
        ExpressionCache expressionCache)
    {
        _parentEngine = parentEngine;
        _config = config;
        _disposalToken = new DisposalToken();
        _context = parentContext.CreateChild();
        _typeMetadata = config.TypeMetadata;
        _expressionCache = expressionCache;
        _pendingVariables = new Dictionary<string, PendingVariable>(config.Comparer);
    }

    private bool IsDisposed()
    {
        if (_disposalToken.IsDisposed)
            return true;

        return _parentEngine?.IsDisposed() ?? false;
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

        Dictionary<Type, TypedDispatch>? typeDispatch = null;
        Dictionary<Type, Func<object, Delegate>>? delegateFactories = null;
        List<TypedDispatch>? extensionDispatches = null;

        if (options.Aot.BuiltInContext != null)
        {
            typeDispatch = new Dictionary<Type, TypedDispatch>();
            foreach (var metadata in options.Aot.BuiltInContext.GetTypeMetadata())
                typeDispatch[metadata.Type] = metadata;

            var factories = options.Aot.BuiltInContext.GetDelegateFactories();
            if (factories != null)
            {
                delegateFactories = new Dictionary<Type, Func<object, Delegate>>();
                foreach (var kvp in factories)
                    delegateFactories[kvp.Key] = kvp.Value;
            }

            var extDispatches = options.Aot.BuiltInContext.GetExtensionDispatches();
            if (extDispatches != null)
                extensionDispatches = new List<TypedDispatch>(extDispatches);
        }

        foreach (var ctx in options.Aot.AdditionalContexts)
        {
            typeDispatch ??= new Dictionary<Type, TypedDispatch>();
            foreach (var metadata in ctx.GetTypeMetadata())
                typeDispatch[metadata.Type] = metadata;

            var factories = ctx.GetDelegateFactories();
            if (factories != null)
            {
                delegateFactories ??= new Dictionary<Type, Func<object, Delegate>>();
                foreach (var kvp in factories)
                    delegateFactories[kvp.Key] = kvp.Value;
            }

            var extDispatches = ctx.GetExtensionDispatches();
            if (extDispatches != null)
            {
                extensionDispatches ??= new List<TypedDispatch>();
                extensionDispatches.AddRange(extDispatches);
            }
        }

        var typeMetadata = new TypeMetadataProvider();
        var preferResolvedRuntimeDispatch =
            !MethodDispatchCache.DynamicCodeSupported ||
            options.Aot.AdditionalContexts.Count > 0;

        return new AlderConfig(
            options.LanguageMode,
            options.Sandbox.ToSecurityPolicy(),
            options.IsCaseSensitive,
            options.Constraints,
            options.Compiler,
            options.ExpressionCompiler,
            options.ServiceProvider,
            preferResolvedRuntimeDispatch,
            Runtime.Collections.FixedDictionary<string, Func<object?[], object?>>.Create(functions),
            Runtime.Collections.FixedDictionary<string, ModuleInfo>.Create(modules),
            [..options.Types.ExtensionTypes],
            typeMetadata,
            typeResolver,
            typeDispatch != null ? Runtime.Collections.FixedDictionary<Type, TypedDispatch>.Create(typeDispatch) : null,
            delegateFactories,
            extensionDispatches?.ToArray());
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

    private BoundExpr RunPipeline(BoundExpr tree, CancellationToken ct = default)
    {
        var context = new Pipeline.PipelineContext(_config.Security, ct);
        return InterpretationPipeline.Execute(tree, context);
    }

    private BoundExpr RunSecurityOnlyPipeline(BoundExpr tree, CancellationToken ct = default)
    {
        var context = new Pipeline.PipelineContext(_config.Security, ct);
        return SecurityOnlyPipeline.Execute(tree, context);
    }

    private BoundExpr RunCompilationPipeline(BoundExpr tree, CancellationToken ct = default)
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

    private static bool ShouldRethrowTryApiException(Exception ex) =>
        ex is OperationCanceledException or ObjectDisposedException;

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

    private static object? UnwrapControlFlowSignal(object? result)
    {
        if (result is not ControlFlowSignal signal)
            return result;

        // §13.10.4: a goto signal that escapes the top-level block has no target label in scope.
        if (signal.SignalKind == ControlFlowSignal.Kind.Goto)
            throw new AlderException(DiagnosticDescriptors.LabelNotFound, (string)signal.Value!);

        return signal.Value;
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
        return new AlderEngine(this, _config, parentContext, _expressionCache);
    }
}

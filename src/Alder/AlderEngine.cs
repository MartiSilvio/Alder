using System.Runtime.CompilerServices;
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
        _config = AlderConfigFactory.Build(options);
        _typeMetadata = _config.TypeMetadata;
        _pendingVariables = new Dictionary<string, PendingVariable>(_config.Comparer);
    }

    internal bool HasCompiler => _config.Compiler != null;

    private AlderEngine(
        AlderEngine parentEngine,
        AlderConfig config,
        AlderContext parentContext)
    {
        _parentEngine = parentEngine;
        _config = config;
        _disposalToken = new DisposalToken();
        _context = parentContext.CreateChild();
        _typeMetadata = config.TypeMetadata;
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

    private AlderContext CreateEvaluationBindingContext(IDictionary<string, object?>? variables)
    {
        var context = GetOrCreateContext();
        if (variables == null || variables.Count == 0)
            return context;

        var scoped = context.CreateChild();
        foreach (var (name, value) in variables)
            scoped.Define(name, value, typeof(object));
        return scoped;
    }

    private AlderContext CreateEvaluationBindingContext((string Name, object? Value, Type Type)[] variables)
    {
        var context = GetOrCreateContext();
        if (variables.Length == 0)
            return context;

        var scoped = context.CreateChild();
        foreach (var (name, value, type) in variables)
            scoped.Define(name, value, type);
        return scoped;
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
        {
            var ex = new AlderException(DiagnosticDescriptors.LabelNotFound, (string)signal.Value!);
            if (!signal.Span.IsEmpty)
                ex.EnrichDiagnosticsWithPosition(signal.Span, null, null);
            throw ex;
        }

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
        return new AlderEngine(this, _config, parentContext);
    }
}

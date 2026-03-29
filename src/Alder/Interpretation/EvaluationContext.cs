using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Parsing;
using Alder.Runtime;
using Alder.Runtime.Semantics;
using Alder.Tracing;

namespace Alder.Interpretation;

internal sealed class EvaluationContext
{
    public AlderContext Context
    {
        get => _contextRef;
        set => _contextRef = value;
    }

    public AlderConfig Config { get; }
    public ExecutionConstraintState? ConstraintState { get; }
    public CancellationToken CancellationToken { get; }
    public Stack<Exception> CaughtExceptions { get; }

    public EvaluationTracer? Tracer { get; set; }
    public int BreakContextDepth { get; set; }
    public int LoopDepth { get; set; }
    public bool IsChecked { get; set; }

    private AlderContext _contextRef;
    private readonly Func<BoundExpr, object?> _evaluate;
    private readonly Dictionary<BoundNodeKind, Func<BoundExpr, EvaluationContext, object?>> _evaluators = new();

    internal EvaluationContext(
        AlderContext context,
        AlderConfig config,
        ExecutionConstraintState? constraintState,
        CancellationToken cancellationToken,
        Stack<Exception> caughtExceptions,
        Func<BoundExpr, object?> evaluate)
    {
        _contextRef = context;
        Config = config;
        ConstraintState = constraintState;
        CancellationToken = cancellationToken;
        CaughtExceptions = caughtExceptions;
        _evaluate = evaluate;
    }

    internal void Register<TNode>(BoundNodeKind kind, INodeEvaluator<TNode> evaluator) where TNode : BoundExpr
    {
        _evaluators[kind] = (expr, ctx) => evaluator.Evaluate((TNode)expr, ctx);
    }

    public object? Evaluate(BoundExpr expr) => _evaluate(expr);

    internal bool TryEvaluate(BoundExpr expr, out object? result)
    {
        if (_evaluators.TryGetValue(expr.Kind, out var evaluator))
        {
            result = evaluator(expr, this);
            return true;
        }
        result = default;
        return false;
    }

    public object? MatchPattern(object? value, Pattern pattern)
        => PatternRuntime.MatchPattern(value, pattern, Context, Config, CancellationToken);
}

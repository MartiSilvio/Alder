using System.Collections;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Runtime;
using Alder.Runtime.Extensions;
using Alder.Runtime.Semantics;

namespace Alder.Interpretation;

internal sealed partial class BoundEvaluator
{
    private ControlFlowSignal? EvaluateWhile(BoundWhileExpr whileExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var iterationContext = _context.CreateChild();

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            while (TypeHelpers.RequireBoolean(Evaluate(whileExpr.Condition)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                iterationContext.ClearScope();

                var previousContext = _context;
                _context = iterationContext;

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(whileExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            }

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }

    private object? EvaluateFor(BoundForExpr forExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var loopContext = _context;
        _context = _context.CreateChild();
        var bodyContext = _context.CreateChild();

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            foreach (var initializer in forExpr.Initializers)
            {
                Evaluate(initializer);
            }

            while (forExpr.Condition == null || TypeHelpers.RequireBoolean(Evaluate(forExpr.Condition)))
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                bodyContext.ClearScope();

                var previousContext = _context;
                _context = bodyContext;

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(forExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind != ControlFlowSignal.Kind.Continue) return signal;
                }

                foreach (var increment in forExpr.Increments)
                {
                    Evaluate(increment);
                }
            }
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
            _context = loopContext;
        }

        return null;
    }

    private object? EvaluateDoWhile(BoundDoWhileExpr doWhileExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var iterationContext = _context.CreateChild();

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            do
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                iterationContext.ClearScope();

                var previousContext = _context;
                _context = iterationContext;

                ControlFlowSignal? signal;
                try
                {
                    signal = ExecuteStatementBlock(doWhileExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            } while (TypeHelpers.RequireBoolean(Evaluate(doWhileExpr.Condition)));

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }

    private object? EvaluateForEach(BoundForEachExpr forEachExpr)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var collection = Evaluate(forEachExpr.Collection);

        collection = RangeHelpers.EnsureEnumerable(collection!);

        if (collection is not IEnumerable enumerable)
        {
            throw new AlderException(DiagnosticDescriptors.ForeachRequiresIEnumerable, TypeNameFormatter.Of(collection));
        }

        _breakContextDepth++;
        _loopDepth++;
        try
        {
            foreach (var item in enumerable)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);

                var previousContext = _context;
                _context = _context.CreateChild();

                ControlFlowSignal? signal;
                try
                {
                    _context.DefineNew(forEachExpr.VariableName, item, forEachExpr.ElementType);
                    signal = ExecuteStatementBlock(forEachExpr.Body);
                }
                finally
                {
                    _context = previousContext;
                }

                if (signal != null)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Break) break;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Continue) continue;
                    return signal;
                }
            }

            return null;
        }
        finally
        {
            _loopDepth--;
            _breakContextDepth--;
        }
    }
}

using CsEval.Binding;
using CsEval.Binding.BoundNodes;
using CsEval.Parsing;
using CsEval.Diagnostics;
using CsEval.Runtime;
using CsEval.Runtime.Semantics;
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;

namespace CsEval.Interpretation;

internal sealed partial class BoundEvaluator
{
    private object? EvaluateBlock(BoundBlockExpr block)
    {
        var constraintState = _context.ConstraintState;
        var constraints = _options.Constraints;
        var previousContext = _context;
        _context = _context.CreateChild();

        try
        {
            var startIndex = 0;
            ExecuteBlock:
            for (var i = startIndex; i < block.Statements.Length; i++)
            {
                ExecutionRuntime.CheckExecutionConstraints(constraintState, constraints, _cancellationToken);
                var result = Evaluate(block.Statements[i]);
                if (result is ControlFlowSignal signal)
                {
                    if (signal.SignalKind == ControlFlowSignal.Kind.Return)
                        return signal;
                    if (signal.SignalKind == ControlFlowSignal.Kind.Goto)
                    {
                        var labelName = (string)signal.Value!;
                        var labelIndex = FindLabelIndex(block.Statements, labelName);
                        if (labelIndex >= 0)
                        {
                            startIndex = labelIndex + 1;
                            goto ExecuteBlock;
                        }
                    }
                    return result;
                }
            }

            return block.ReturnExpr != null ? Evaluate(block.ReturnExpr) : null;
        }
        finally
        {
            _context = previousContext;
        }
    }

    private object? EvaluateVariableDecl(BoundVariableDeclExpr variableDecl)
    {
        var value = Evaluate(variableDecl.Initializer);
        return AssignmentRuntime.DefineVariable(
            variableDecl.Name,
            value,
            variableDecl.DeclaredType,
            _context,
            variableDecl.IsConst);
    }

    private object? EvaluateIfStatement(BoundIfStatementExpr ifStatement)
    {
        var condition = Evaluate(ifStatement.Condition);
        if (TypeHelpers.RequireBoolean(condition))
        {
            return EvaluateBranch(ifStatement.ThenStatements);
        }

        if (!ifStatement.ElseStatements.IsDefaultOrEmpty)
        {
            return EvaluateBranch(ifStatement.ElseStatements);
        }

        return null;
    }

    private object? EvaluateBranch(IEnumerable<BoundExpr> statements)
    {
        var previousContext = _context;
        _context = _context.CreateChild();

        try
        {
            foreach (var statement in statements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var result = Evaluate(statement);
                if (result is ControlFlowSignal)
                    return result;
            }

            return null;
        }
        finally
        {
            _context = previousContext;
        }
    }

    private object? EvaluateSwitch(BoundSwitchStatementExpr switchStatement)
    {
        var switchValue = Evaluate(switchStatement.Expression);
        var matched = false;
        var defaultCaseIndex = -1;

        _breakContextDepth++;
        try
        {
            for (var i = 0; i < switchStatement.Cases.Length; i++)
            {
                var switchCase = switchStatement.Cases[i];
                if (switchCase.CasePattern == null)
                {
                    defaultCaseIndex = i;
                    continue;
                }

                if (matched)
                    continue;

                var previousContext = _context;
                _context = _context.CreateChild();
                try
                {
                    if (!TypeHelpers.RequireBoolean(MatchPattern(switchValue, switchCase.CasePattern)))
                        continue;

                    if (switchCase.WhenGuard != null)
                    {
                        var guardResult = Evaluate(switchCase.WhenGuard);
                        if (!TypeHelpers.RequireBoolean(guardResult))
                            continue;
                    }

                    matched = true;
                    var signal = ExecuteSwitchCaseWithGoto(switchStatement, i);
                    if (signal != null)
                        return signal.SignalKind == ControlFlowSignal.Kind.Break ? null : signal;
                }
                finally
                {
                    _context = previousContext;
                }
            }

            if (!matched && defaultCaseIndex >= 0)
            {
                var signal = ExecuteSwitchCaseWithGoto(switchStatement, defaultCaseIndex);
                if (signal != null && signal.SignalKind != ControlFlowSignal.Kind.Break)
                    return signal;
            }

            return null;
        }
        finally
        {
            _breakContextDepth--;
        }
    }

    private ControlFlowSignal? ExecuteSwitchCaseWithGoto(BoundSwitchStatementExpr switchStatement, int startIndex)
    {
        var signal = ExecuteSwitchCaseStatements(switchStatement.Cases, startIndex);
        while (signal != null && signal.SignalKind is ControlFlowSignal.Kind.GotoCase or ControlFlowSignal.Kind.GotoDefault)
        {
            int targetIndex;
            if (signal.SignalKind == ControlFlowSignal.Kind.GotoDefault)
            {
                targetIndex = FindDefaultCaseIndex(switchStatement.Cases);
            }
            else
            {
                targetIndex = FindCaseIndex(switchStatement, signal.Value);
            }
            if (targetIndex < 0)
                throw new CsEvalException(DiagnosticDescriptors.GotoCaseTargetNotFound);
            signal = ExecuteSwitchCaseStatements(switchStatement.Cases, targetIndex);
        }
        return signal;
    }

    private object? EvaluateSwitchExpression(BoundSwitchExpressionExpr switchExpression)
    {
        var value = Evaluate(switchExpression.Expression);

        foreach (var arm in switchExpression.Arms)
        {
            var previousContext = _context;
            _context = _context.CreateChild();

            try
            {
                if (!TypeHelpers.RequireBoolean(MatchPattern(value, arm.Pattern)))
                    continue;

                if (arm.WhenGuard != null)
                {
                    var guardResult = Evaluate(arm.WhenGuard);
                    if (!TypeHelpers.RequireBoolean(guardResult))
                        continue;
                }

                return Evaluate(arm.Value);
            }
            finally
            {
                _context = previousContext;
            }
        }

        throw new CsEvalException(DiagnosticDescriptors.SwitchExpressionNonExhaustive, value ?? "null");
    }

    private ControlFlowSignal? ExecuteStatementBlock(IEnumerable<BoundExpr> statements)
    {
        foreach (var statement in statements)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            var result = Evaluate(statement);
            if (result is ControlFlowSignal signal)
                return signal;
        }

        return null;
    }

    private ControlFlowSignal? ExecuteSwitchCaseStatements(IReadOnlyList<BoundSwitchCase> cases, int startIndex)
    {
        for (var i = startIndex; i < cases.Count; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            foreach (var statement in switchCase.Statements)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                var result = Evaluate(statement);
                if (result is ControlFlowSignal signal)
                    return signal;
            }

            throw new CsEvalException(DiagnosticDescriptors.CaseFallThrough);
        }

        return null;
    }

    private object? EvaluateBreak()
    {
        if (_breakContextDepth == 0)
            throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Break;
    }

    private object? EvaluateContinue()
    {
        if (_loopDepth == 0)
            throw new CsEvalException(DiagnosticDescriptors.BreakOrContinueOutsideLoop);

        return ControlFlowSignal.Continue;
    }

    private object? EvaluateThrow(BoundThrowExpr throwExpr)
    {
        var result = Evaluate(throwExpr.Expression);
        var exception = ExecutionRuntime.ValidateThrowOperand(result);
        throw exception;
    }

    private object? EvaluateThrowStatement()
    {
        if (_caughtExceptions.Count == 0)
            throw new CsEvalException(DiagnosticDescriptors.ThrowOutsideCatch);

        ExceptionDispatchInfo.Capture(_caughtExceptions.Peek()).Throw();
        return null;
    }

    private object? EvaluateTryCatchFinally(BoundTryCatchFinallyExpr tryCatchFinally)
    {
        object? result = null;
        Exception? unhandledException = null;
        ControlFlowSignal? pendingSignal = null;

        try
        {
            foreach (var statement in tryCatchFinally.TryBody)
            {
                result = Evaluate(statement);
                if (result is ControlFlowSignal signal)
                {
                    pendingSignal = signal;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            var (handled, catchResult, catchSignal) = TryMatchCatchClause(tryCatchFinally.CatchClauses, ex);
            if (handled)
            {
                result = catchResult;
                pendingSignal = catchSignal;
            }
            else
            {
                unhandledException = ex;
            }
        }
        finally
        {
            foreach (var statement in tryCatchFinally.FinallyBody)
            {
                Evaluate(statement);
            }
        }

        if (unhandledException != null)
            ExceptionDispatchInfo.Capture(unhandledException).Throw();

        if (pendingSignal != null)
            return pendingSignal;

        return result;
    }

    private (bool Handled, object? Result, ControlFlowSignal? Signal) TryMatchCatchClause(
        IReadOnlyList<BoundCatchClause> catchClauses,
        Exception ex)
    {
        foreach (var catchClause in catchClauses)
        {
            if (catchClause.ExceptionTypeName != null)
            {
                var catchType = _context.TypeResolver.ResolveType(catchClause.ExceptionTypeName);
                if (!catchType.IsInstanceOfType(ex))
                    continue;
            }

            var previousContext = _context;
            _context = _context.CreateChild();
            try
            {
                if (catchClause.VariableName != null)
                    _context.DefineNew(catchClause.VariableName, ex, ex.GetType());

                if (catchClause.WhenGuard != null)
                {
                    bool guardMatched;
                    try
                    {
                        var guardResult = Evaluate(catchClause.WhenGuard);
                        guardMatched = TypeHelpers.RequireBoolean(guardResult);
                    }
                    catch
                    {
                        guardMatched = false;
                    }

                    if (!guardMatched)
                        continue;
                }

                _caughtExceptions.Push(ex);
                try
                {
                    object? result = null;
                    ControlFlowSignal? signal = null;
                    foreach (var statement in catchClause.Body)
                    {
                        result = Evaluate(statement);
                        if (result is ControlFlowSignal controlFlowSignal)
                        {
                            signal = controlFlowSignal;
                            break;
                        }
                    }

                    return (true, result, signal);
                }
                finally
                {
                    _caughtExceptions.Pop();
                }
            }
            finally
            {
                _context = previousContext;
            }
        }

        return (false, null, null);
    }

    private ControlFlowSignal? EvaluateReturn(BoundReturnExpr returnExpr)
    {
        var value = returnExpr.Value != null ? Evaluate(returnExpr.Value) : null;
        return ControlFlowSignal.Return(value);
    }

    private object? EvaluateUsingStatement(BoundUsingStatementExpr usingStatement)
    {
        var resource = Evaluate(usingStatement.Resource);
        try
        {
            return Evaluate(usingStatement.Body);
        }
        finally
        {
            if (resource is IDisposable disposable)
                disposable.Dispose();
            else if (resource is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private object? EvaluateLockStatement(BoundLockStatementExpr lockStatement)
    {
        var lockObject = Evaluate(lockStatement.LockObject);
        if (lockObject == null)
            throw new CsEvalException(DiagnosticDescriptors.LockRequiresNonNull);

        lock (lockObject)
        {
            return Evaluate(lockStatement.Body);
        }
    }

    private static int FindLabelIndex(ImmutableArray<BoundExpr> statements, string label)
    {
        for (var i = 0; i < statements.Length; i++)
            if (statements[i] is BoundLabelExpr l && l.Name == label)
                return i;
        return -1;
    }

    private static int FindDefaultCaseIndex(IReadOnlyList<BoundSwitchCase> cases)
    {
        for (var i = 0; i < cases.Count; i++)
            if (cases[i].CasePattern == null)
                return i;
        return -1;
    }

    private int FindCaseIndex(BoundSwitchStatementExpr switchStatement, object? targetValue)
    {
        for (var i = 0; i < switchStatement.Cases.Length; i++)
        {
            var casePattern = switchStatement.Cases[i].CasePattern;
            if (casePattern is ConstantPattern cp)
            {
                var caseValue = MatchPattern(targetValue, cp);
                if (TypeHelpers.RequireBoolean(caseValue))
                    return i;
            }
        }
        return -1;
    }
}

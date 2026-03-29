using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Diagnostics;
using Alder.Interpretation;
using Alder.Parsing;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private LinqExpression EmitBlock(BoundBlockExpr block)
    {
        var statements = block.Statements;
        var hasLabels = statements.Any(s => s is BoundLabelExpr);

        if (hasLabels)
            return EmitBlockWithLabels(block);

        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "prevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "blockResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "blockSignal");
        var doneLabel = LinqExpression.Label("blockDone");

        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(body, statements, resultVar, signalVar, doneLabel, unwrapReturnSignal: false);
        if (block.ReturnExpr != null)
            body.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(block.ReturnExpr))));
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(_contextParam, previousContextVar)),
            resultVar);
    }

    private LinqExpression EmitBlockWithLabels(BoundBlockExpr block)
    {
        var statements = block.Statements;
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "prevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "blockResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "blockSignal");
        var startIndexVar = LinqExpression.Variable(typeof(int), "gotoStartIndex");
        var doneLabel = LinqExpression.Label("blockDone");
        var loopBreak = LinqExpression.Label("blockLoopBreak");
        var loopContinue = LinqExpression.Label("blockLoopContinue");

        var labelIndices = new Dictionary<string, int>();
        for (var i = 0; i < statements.Length; i++)
            if (statements[i] is BoundLabelExpr label)
                labelIndices[label.Name] = i;

        var loopBody = new List<LinqExpression>();

        for (var i = 0; i < statements.Length; i++)
        {
            var stmtBody = new List<LinqExpression>
            {
                LinqExpression.Call(
                    CheckExecutionConstraintsMethod,
                    _constraintStateParam,
                    LinqExpression.Property(_configParam, nameof(AlderConfig.Constraints)),
                    _ctParam),
                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(statements[i]))),
                LinqExpression.IfThen(
                    LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
                    LinqExpression.Block(
                        LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                        BuildBlockGotoCheck(signalVar, resultVar, startIndexVar, loopContinue, labelIndices),
                        LinqExpression.Goto(doneLabel)))
            };

            loopBody.Add(LinqExpression.IfThen(
                LinqExpression.LessThanOrEqual(startIndexVar, LinqExpression.Constant(i)),
                LinqExpression.Block(typeof(void), stmtBody)));
        }

        loopBody.Add(LinqExpression.Break(loopBreak));

        var outerBody = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(startIndexVar, LinqExpression.Constant(0))
        };

        outerBody.Add(LinqExpression.Loop(
            LinqExpression.Block(typeof(void), loopBody),
            loopBreak,
            loopContinue));

        if (block.ReturnExpr != null)
            outerBody.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(block.ReturnExpr))));
        outerBody.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar, startIndexVar],
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(outerBody),
                LinqExpression.Assign(_contextParam, previousContextVar)),
            resultVar);
    }

    private static LinqExpression BuildBlockGotoCheck(
        ParameterExpression signalVar,
        ParameterExpression resultVar,
        ParameterExpression startIndexVar,
        LabelTarget loopContinue,
        Dictionary<string, int> labelIndices)
    {
        LinqExpression check = LinqExpression.Empty();
        var kindExpr = LinqExpression.Property(signalVar, ControlFlowSignalKindProperty);
        var valueExpr = LinqExpression.Property(signalVar, ControlFlowValueProperty);

        foreach (var (label, index) in labelIndices)
        {
            check = LinqExpression.IfThen(
                LinqExpression.AndAlso(
                    LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Goto)),
                    LinqExpression.Call(
                        typeof(string).GetMethod(nameof(string.Equals), [typeof(string), typeof(string)])!,
                        LinqExpression.Convert(valueExpr, typeof(string)),
                        LinqExpression.Constant(label))),
                LinqExpression.Block(
                    LinqExpression.Assign(startIndexVar, LinqExpression.Constant(index + 1)),
                    LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Continue(loopContinue)));
        }

        return check;
    }

    private LinqExpression EmitIfStatement(BoundIfStatementExpr ifStatement)
    {
        var resultVar = LinqExpression.Variable(typeof(object), "ifResult");
        var condition = EmitBoolCondition(ifStatement.Condition);
        var thenBody = EmitScopedStatements(ifStatement.ThenStatements);
        var elseBody = ifStatement.ElseStatements.IsDefaultOrEmpty
            ? LinqExpression.Constant(null, typeof(object))
            : EmitScopedStatements(ifStatement.ElseStatements);

        return LinqExpression.Block(
            typeof(object),
            [resultVar],
            LinqExpression.Assign(
                resultVar,
                LinqExpression.Condition(condition, EmitHelpers.AsObject(thenBody), EmitHelpers.AsObject(elseBody))),
            resultVar);
    }

    private LinqExpression EmitWhile(BoundWhileExpr whileExpr)
    {
        var loopBreakLabel = LinqExpression.Label(typeof(object), "whileBreak");
        var loopContinueLabel = LinqExpression.Label("whileContinue");
        var resultVar = LinqExpression.Variable(typeof(object), "whileResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "whileSignal");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.IfThen(
                LinqExpression.Not(EmitBoolCondition(whileExpr.Condition)),
                LinqExpression.Break(loopBreakLabel, resultVar))
        };

        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            EmitLoopIterationBody(body, whileExpr.Body, resultVar, signalVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: true);
            body.Add(LinqExpression.Label(loopContinueLabel));

            return LinqExpression.Block(
                typeof(object),
                [resultVar, signalVar],
                LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel),
                resultVar);
        }
        finally
        {
            _loopDepth = previousDepth;
        }
    }

    private LinqExpression EmitFor(BoundForExpr forExpr)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "forPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "forResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "forSignal");
        var loopBreakLabel = LinqExpression.Label(typeof(object), "forBreak");
        var loopContinueLabel = LinqExpression.Label("forContinue");

        var prologue = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod))
        };

        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            for (var i = 0; i < forExpr.Initializers.Length; i++)
                prologue.Add(EmitHelpers.AsObject(Emit(forExpr.Initializers[i])));

            var body = new List<LinqExpression>();
            if (forExpr.Condition != null)
            {
                body.Add(LinqExpression.IfThen(
                    LinqExpression.Not(EmitBoolCondition(forExpr.Condition)),
                    LinqExpression.Break(loopBreakLabel, resultVar)));
            }

            EmitLoopIterationBody(body, forExpr.Body, resultVar, signalVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: false);
            body.Add(LinqExpression.Label(loopContinueLabel));
            for (var i = 0; i < forExpr.Increments.Length; i++)
                body.Add(EmitHelpers.AsObject(Emit(forExpr.Increments[i])));

            return LinqExpression.Block(
                typeof(object),
                [previousContextVar, resultVar, signalVar],
                LinqExpression.TryFinally(
                    LinqExpression.Block(
                        prologue.Append(
                            LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel))),
                    LinqExpression.Assign(_contextParam, previousContextVar)),
                resultVar);
        }
        finally
        {
            _loopDepth = previousDepth;
        }
    }

    private LinqExpression EmitDoWhile(BoundDoWhileExpr doWhileExpr)
    {
        var loopBreakLabel = LinqExpression.Label(typeof(object), "doBreak");
        var loopContinueLabel = LinqExpression.Label("doContinue");
        var resultVar = LinqExpression.Variable(typeof(object), "doResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "doSignal");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            EmitLoopIterationBody(body, doWhileExpr.Body, resultVar, signalVar, loopBreakLabel, loopContinueLabel, hasConditionCheck: false);
            body.Add(LinqExpression.Label(loopContinueLabel));
            body.Add(LinqExpression.IfThen(
                LinqExpression.Not(EmitBoolCondition(doWhileExpr.Condition)),
                LinqExpression.Break(loopBreakLabel, resultVar)));

            return LinqExpression.Block(
                typeof(object),
                [resultVar, signalVar],
                LinqExpression.Loop(LinqExpression.Block(body), loopBreakLabel),
                resultVar);
        }
        finally
        {
            _loopDepth = previousDepth;
        }
    }

    private LinqExpression EmitForEach(BoundForEachExpr forEachExpr)
    {
        var enumerableVar = LinqExpression.Variable(typeof(object), "foreachCollection");
        var enumeratorVar = LinqExpression.Variable(typeof(IEnumerator), "foreachEnumerator");
        var resultVar = LinqExpression.Variable(typeof(object), "foreachResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "foreachSignal");
        var currentVar = LinqExpression.Variable(typeof(object), "foreachCurrent");
        var loopBreakLabel = LinqExpression.Label(typeof(object), "foreachBreak");
        var loopContinueLabel = LinqExpression.Label("foreachContinue");

        List<LinqExpression> loopBody;
        var previousDepth = _loopDepth;
        _loopDepth = previousDepth + 1;
        try
        {
            var iterationBody = EmitForeachIteration(forEachExpr.VariableName, currentVar, forEachExpr.Body, forEachExpr.ElementType);
            loopBody = new List<LinqExpression>
            {
                LinqExpression.Call(
                    CheckExecutionConstraintsMethod,
                    _constraintStateParam,
                    LinqExpression.Property(_configParam, nameof(AlderConfig.Constraints)),
                    _ctParam),
                LinqExpression.Call(
                    CheckLoopIterationConstraintMethod,
                    _constraintStateParam,
                    LinqExpression.Property(_configParam, nameof(AlderConfig.Constraints))),
                LinqExpression.IfThen(
                    LinqExpression.Not(LinqExpression.Call(enumeratorVar, MoveNextMethod)),
                    LinqExpression.Break(loopBreakLabel, resultVar)),
                LinqExpression.Assign(currentVar, LinqExpression.Convert(LinqExpression.Call(enumeratorVar, GetCurrentMethod), typeof(object))),
                LinqExpression.Assign(resultVar, iterationBody),
                BuildLoopSignalDispatch(resultVar, signalVar, loopBreakLabel, loopContinueLabel),
                LinqExpression.Label(loopContinueLabel)
            };
        }
        finally
        {
            _loopDepth = previousDepth;
        }

        var disposableVar = LinqExpression.Variable(typeof(IDisposable), "foreachDisposable");
        var disposeBlock = LinqExpression.Block(
            LinqExpression.Assign(disposableVar, LinqExpression.TypeAs(enumeratorVar, typeof(IDisposable))),
            LinqExpression.IfThen(
                LinqExpression.NotEqual(disposableVar, LinqExpression.Constant(null, typeof(IDisposable))),
                LinqExpression.Call(disposableVar, DisposeMethod)));

        return LinqExpression.Block(
            typeof(object),
            [enumerableVar, enumeratorVar, resultVar, signalVar, currentVar, disposableVar],
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(enumerableVar, LinqExpression.Call(EnsureEnumerableMethod, EmitHelpers.AsObject(Emit(forEachExpr.Collection)))),
            LinqExpression.Assign(enumeratorVar, LinqExpression.Call(GetEnumeratorMethod, enumerableVar)),
            LinqExpression.TryFinally(
                LinqExpression.Loop(LinqExpression.Block(loopBody), loopBreakLabel),
                disposeBlock),
            resultVar);
    }

    private LinqExpression EmitUsingStatement(BoundUsingStatementExpr usingStatement)
    {
        var resourceVar = LinqExpression.Variable(typeof(object), "usingResource");
        var resultVar = LinqExpression.Variable(typeof(object), "usingResult");

        return LinqExpression.Block(
            typeof(object),
            [resourceVar, resultVar],
            LinqExpression.Assign(resourceVar, EmitHelpers.AsObject(Emit(usingStatement.Resource))),
            LinqExpression.TryFinally(
                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(usingStatement.Body))),
                LinqExpression.Call(DisposeResourceMethod, resourceVar)),
            resultVar);
    }

    private LinqExpression EmitLockStatement(BoundLockStatementExpr lockStatement)
    {
        var lockObjVar = LinqExpression.Variable(typeof(object), "lockObj");
        var resultVar = LinqExpression.Variable(typeof(object), "lockResult");

        return LinqExpression.Block(
            typeof(object),
            [lockObjVar, resultVar],
            LinqExpression.Assign(
                lockObjVar,
                LinqExpression.Call(ValidateLockObjectMethod, EmitHelpers.AsObject(Emit(lockStatement.LockObject)))),
            LinqExpression.Call(MonitorEnterMethod, lockObjVar),
            LinqExpression.TryFinally(
                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(lockStatement.Body))),
                LinqExpression.Call(MonitorExitMethod, lockObjVar)),
            resultVar);
    }

    private LinqExpression EmitBreak(BoundBreakExpr _)
    {
        if (_loopDepth > 0 || _switchDepth > 0)
            return LinqExpression.Convert(LinqExpression.Field(null, ControlFlowBreakField), typeof(object));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(object));
    }

    private LinqExpression EmitContinue(BoundContinueExpr _)
    {
        if (_loopDepth > 0)
            return LinqExpression.Convert(LinqExpression.Field(null, ControlFlowContinueField), typeof(object));

        return LinqExpression.Throw(
            LinqExpression.Constant(new AlderException(DiagnosticDescriptors.BreakOrContinueOutsideLoop)),
            typeof(object));
    }

    private LinqExpression EmitGoto(BoundGotoExpr gotoExpr)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(ControlFlowGotoMethod, LinqExpression.Constant(gotoExpr.Label)),
            typeof(object));
    }

    private LinqExpression EmitGotoCase(BoundGotoCaseExpr gotoCaseExpr)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(ControlFlowGotoCaseMethod, EmitHelpers.AsObject(Emit(gotoCaseExpr.Value))),
            typeof(object));
    }

    private LinqExpression EmitGotoDefault()
    {
        return LinqExpression.Convert(
            LinqExpression.Field(null, ControlFlowGotoDefaultField),
            typeof(object));
    }

    private LinqExpression EmitReturn(BoundReturnExpr returnExpr)
    {
        return LinqExpression.Convert(
            LinqExpression.Call(
                ControlFlowReturnMethod,
                returnExpr.Value == null
                    ? LinqExpression.Constant(null, typeof(object))
                    : EmitHelpers.AsObject(Emit(returnExpr.Value))),
            typeof(object));
    }

    private LinqExpression EmitTryCatchFinally(BoundTryCatchFinallyExpr tryCatchFinally)
    {
        var tryBody = EmitStatementSequence(tryCatchFinally.TryBody);
        var catchBlocks = new List<CatchBlock>(tryCatchFinally.CatchClauses.Length);

        for (var i = 0; i < tryCatchFinally.CatchClauses.Length; i++)
        {
            var catchClause = tryCatchFinally.CatchClauses[i];
            var exParam = LinqExpression.Parameter(typeof(Exception), $"catchEx{i}");
            var catchBody = EmitCatchClauseBody(catchClause, exParam);
            var filter = BuildCatchFilter(catchClause, exParam);
            catchBlocks.Add(LinqExpression.MakeCatchBlock(typeof(Exception), exParam, catchBody, filter));
        }

        LinqExpression? finallyBody = null;
        if (!tryCatchFinally.FinallyBody.IsDefaultOrEmpty)
        {
            var statements = new List<LinqExpression>(tryCatchFinally.FinallyBody.Length);
            for (var i = 0; i < tryCatchFinally.FinallyBody.Length; i++)
                statements.Add(EmitHelpers.AsObject(Emit(tryCatchFinally.FinallyBody[i])));
            finallyBody = LinqExpression.Block(statements);
        }

        if (catchBlocks.Count > 0 && finallyBody != null)
            return LinqExpression.TryCatchFinally(tryBody, finallyBody, catchBlocks.ToArray());
        if (catchBlocks.Count > 0)
            return LinqExpression.TryCatch(tryBody, catchBlocks.ToArray());
        if (finallyBody != null)
            return LinqExpression.TryFinally(tryBody, finallyBody);

        return tryBody;
    }

    private LinqExpression EmitCatchClauseBody(BoundCatchClause catchClause, ParameterExpression exParam)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "catchPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "catchResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "catchSignal");
        var doneLabel = LinqExpression.Label("catchDone");
        var bodyStatements = new List<LinqExpression>();

        var previousDepth = _catchDepth;
        _catchDepth = previousDepth + 1;
        try
        {
            bodyStatements.Add(LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))));
            EmitStatementListBody(
                bodyStatements,
                catchClause.Body,
                resultVar,
                signalVar,
                doneLabel,
                unwrapReturnSignal: false);
            bodyStatements.Add(LinqExpression.Label(doneLabel));
        }
        finally
        {
            _catchDepth = previousDepth;
        }

        var scopedStatements = new List<LinqExpression>
        {
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod))
        };

        if (catchClause.VariableName != null)
        {
            scopedStatements.Add(
                LinqExpression.Call(
                    _contextParam,
                    ContextDefineNewMethod,
                    LinqExpression.Constant(catchClause.VariableName),
                    LinqExpression.Convert(exParam, typeof(object)),
                    LinqExpression.Call(exParam, typeof(object).GetMethod(nameof(GetType))!),
                    LinqExpression.Constant(false)));
        }

        scopedStatements.Add(
            LinqExpression.TryFinally(
                LinqExpression.Block(bodyStatements),
                LinqExpression.Assign(_contextParam, previousContextVar)));
        scopedStatements.Add(resultVar);

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            scopedStatements);
    }

    private LinqExpression? BuildCatchFilter(BoundCatchClause catchClause, ParameterExpression exParam)
    {
        LinqExpression? typeFilter = null;
        if (catchClause.ExceptionTypeName != null)
        {
            var resolvedType = ResolveTypeByName(catchClause.ExceptionTypeName);
            typeFilter = LinqExpression.Call(
                typeof(TypeHelpers).GetMethod(nameof(TypeHelpers.IsType), [typeof(object), typeof(Type)])!,
                LinqExpression.Convert(exParam, typeof(object)),
                resolvedType);
        }

        LinqExpression? whenFilter = null;
        if (catchClause.WhenGuard != null)
        {
            whenFilter = LinqExpression.Call(
                EvaluateCatchWhenGuardMethod,
                LinqExpression.Constant(catchClause.WhenGuard, typeof(BoundExpr)),
                LinqExpression.Constant(catchClause.VariableName, typeof(string)),
                LinqExpression.Convert(exParam, typeof(object)),
                _contextParam,
                _configParam,
                _ctParam);
        }

        if (typeFilter == null)
            return whenFilter;
        if (whenFilter == null)
            return typeFilter;
        return LinqExpression.AndAlso(typeFilter, whenFilter);
    }

    private LinqExpression EmitSwitchExpression(BoundSwitchExpressionExpr switchExpression)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "switchValue");
        var resultVar = LinqExpression.Variable(typeof(object), "switchExprResult");
        var doneLabel = LinqExpression.Label("switchExprDone");
        var statements = new List<LinqExpression>
        {
            LinqExpression.Assign(valueVar, EmitHelpers.AsObject(Emit(switchExpression.Expression)))
        };

        for (var i = 0; i < switchExpression.Arms.Length; i++)
        {
            var arm = switchExpression.Arms[i];
            var previousContextVar = LinqExpression.Variable(typeof(AlderContext), $"switchArmPrevCtx{i}");
            var armCondition = (LinqExpression)LinqExpression.Call(
                MatchPatternMethod,
                valueVar,
                LinqExpression.Constant(arm.Pattern, typeof(Pattern)),
                _contextParam,
                _configParam,
                _ctParam);

            if (arm.WhenGuard != null)
            {
                armCondition = LinqExpression.AndAlso(
                    armCondition,
                    LinqExpression.Call(RequireBooleanMethod, EmitHelpers.AsObject(Emit(arm.WhenGuard))));
            }

            statements.Add(
                LinqExpression.Block(
                    typeof(void),
                    [previousContextVar],
                    LinqExpression.Assign(previousContextVar, _contextParam),
                    LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
                    LinqExpression.TryFinally(
                        LinqExpression.IfThen(
                            armCondition,
                            LinqExpression.Block(
                                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(arm.Value))),
                                LinqExpression.Goto(doneLabel))),
                        LinqExpression.Assign(_contextParam, previousContextVar))));
        }

        statements.Add(
            LinqExpression.Throw(
                LinqExpression.New(
                    AlderExceptionCtor,
                    LinqExpression.Field(null, SwitchExpressionNonExhaustiveDescriptor),
                    LinqExpression.NewArrayInit(typeof(object),
                        LinqExpression.Coalesce(valueVar, LinqExpression.Constant("null", typeof(object))))),
                typeof(void)));
        statements.Add(LinqExpression.Label(doneLabel));
        statements.Add(resultVar);

        return LinqExpression.Block(typeof(object), [valueVar, resultVar], statements);
    }

    private static readonly object GotoDefaultSentinel = new();

    private LinqExpression EmitSwitchStatement(BoundSwitchStatementExpr switchStatement)
    {
        var valueVar = LinqExpression.Variable(typeof(object), "switchValue");
        var matchedVar = LinqExpression.Variable(typeof(bool), "switchMatched");
        var resultVar = LinqExpression.Variable(typeof(object), "switchResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "switchSignal");
        var doneLabel = LinqExpression.Label("switchDone");
        var loopBreak = LinqExpression.Label("switchLoopBreak");
        var loopContinue = LinqExpression.Label("switchLoopContinue");

        var outerStatements = new List<LinqExpression>
        {
            LinqExpression.Assign(valueVar, EmitHelpers.AsObject(Emit(switchStatement.Expression))),
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        var loopBody = new List<LinqExpression>
        {
            LinqExpression.Assign(matchedVar, LinqExpression.Constant(false))
        };

        var defaultCaseIndex = -1;
        var previousSwitchDepth = _switchDepth;
        _switchDepth = previousSwitchDepth + 1;
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

                var previousContextVar = LinqExpression.Variable(typeof(AlderContext), $"switchPrevCtx{i}");
                var matchCondition = BuildSwitchCaseMatchCondition(valueVar, switchCase);
                var executeCase = EmitSwitchCaseExecution(switchStatement.Cases, i, resultVar, signalVar, doneLabel);

                loopBody.Add(
                    LinqExpression.Block(
                        typeof(void),
                        [previousContextVar],
                        LinqExpression.Assign(previousContextVar, _contextParam),
                        LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
                        LinqExpression.TryFinally(
                            LinqExpression.IfThen(
                                LinqExpression.AndAlso(LinqExpression.Not(matchedVar), matchCondition),
                                LinqExpression.Block(
                                    LinqExpression.Assign(matchedVar, LinqExpression.Constant(true)),
                                    LinqExpression.Assign(resultVar, executeCase),
                                    LinqExpression.Goto(doneLabel))),
                            LinqExpression.Assign(_contextParam, previousContextVar))));
            }

            if (defaultCaseIndex >= 0)
            {
                var executeDefault = EmitSwitchCaseExecution(switchStatement.Cases, defaultCaseIndex, resultVar, signalVar, doneLabel);
                loopBody.Add(
                    LinqExpression.IfThen(
                        LinqExpression.Not(matchedVar),
                        LinqExpression.Block(
                            LinqExpression.Assign(resultVar, executeDefault),
                            LinqExpression.Goto(doneLabel))));
            }

            loopBody.Add(LinqExpression.Label(doneLabel));

            var kindExpr = LinqExpression.Property(signalVar, ControlFlowSignalKindProperty);
            loopBody.Add(
                LinqExpression.IfThen(
                    LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
                    LinqExpression.Block(
                        LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.GotoCase)),
                            LinqExpression.Block(
                                LinqExpression.Assign(valueVar, LinqExpression.Property(signalVar, ControlFlowValueProperty)),
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Continue(loopContinue))),
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.GotoDefault)),
                            LinqExpression.Block(
                                LinqExpression.Assign(valueVar, LinqExpression.Constant(GotoDefaultSentinel)),
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Continue(loopContinue))),
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))))));

            loopBody.Add(LinqExpression.Break(loopBreak));

            outerStatements.Add(LinqExpression.Loop(
                LinqExpression.Block(typeof(void), loopBody),
                loopBreak,
                loopContinue));
            outerStatements.Add(resultVar);
            return LinqExpression.Block(typeof(object), [valueVar, matchedVar, resultVar, signalVar], outerStatements);
        }
        finally
        {
            _switchDepth = previousSwitchDepth;
        }
    }

    private LinqExpression BuildSwitchCaseMatchCondition(
        ParameterExpression valueVar,
        BoundSwitchCase switchCase)
    {
        var patternMatch = LinqExpression.Call(
            MatchPatternMethod,
            valueVar,
            LinqExpression.Constant(switchCase.CasePattern!, typeof(Pattern)),
            _contextParam,
            _configParam,
            _ctParam);

        if (switchCase.WhenGuard == null)
            return patternMatch;

        return LinqExpression.AndAlso(
            patternMatch,
            LinqExpression.Call(RequireBooleanMethod, EmitHelpers.AsObject(Emit(switchCase.WhenGuard))));
    }

    private LinqExpression EmitSwitchCaseExecution(
        ImmutableArray<BoundSwitchCase> cases,
        int startIndex,
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget doneLabel)
    {
        var statements = new List<LinqExpression>();

        for (var i = startIndex; i < cases.Length; i++)
        {
            var switchCase = cases[i];
            if (switchCase.Statements.IsDefaultOrEmpty)
                continue;

            if (!TerminatesControlFlow(switchCase.Statements[^1]))
                throw new AlderException(DiagnosticDescriptors.CaseFallThrough);

            var caseDone = LinqExpression.Label($"switchCaseDone{i}");
            EmitStatementListBody(
                statements,
                switchCase.Statements,
                resultVar,
                signalVar,
                caseDone,
                unwrapReturnSignal: false);
            statements.Add(LinqExpression.Label(caseDone));

            var kindExpr = LinqExpression.Property(signalVar, ControlFlowSignalKindProperty);
            statements.Add(
                LinqExpression.IfThen(
                    LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
                    LinqExpression.Block(
                        LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                        LinqExpression.IfThen(
                            LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                            LinqExpression.Block(
                                LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                                LinqExpression.Goto(doneLabel))),
                        LinqExpression.Goto(doneLabel))));

            statements.Add(
                LinqExpression.Throw(
                    LinqExpression.Constant(new AlderException(DiagnosticDescriptors.CaseFallThrough)),
                    typeof(void)));
            break;
        }

        if (statements.Count == 0)
            return LinqExpression.Constant(null, typeof(object));

        statements.Add(resultVar);
        return LinqExpression.Block(typeof(object), statements);
    }

    private static bool TerminatesControlFlow(BoundExpr expr)
    {
        return expr.Kind switch
        {
            BoundNodeKind.BreakStatement => true,
            BoundNodeKind.ReturnStatement => true,
            BoundNodeKind.ContinueStatement => true,
            BoundNodeKind.GotoStatement => true,
            BoundNodeKind.GotoCaseStatement => true,
            BoundNodeKind.GotoDefaultStatement => true,
            BoundNodeKind.ThrowExpression => true,
            BoundNodeKind.Block when ((BoundBlockExpr)expr).Statements.Length > 0 =>
                TerminatesControlFlow(((BoundBlockExpr)expr).Statements[^1]),
            _ => false
        };
    }

    private LinqExpression EmitStatementSequence(ImmutableArray<BoundExpr> statements)
    {
        var resultVar = LinqExpression.Variable(typeof(object), "tryResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "trySignal");
        var doneLabel = LinqExpression.Label("tryDone");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(
            body,
            statements,
            resultVar,
            signalVar,
            doneLabel,
            unwrapReturnSignal: false);
        body.Add(LinqExpression.Label(doneLabel));
        body.Add(resultVar);

        return LinqExpression.Block(typeof(object), [resultVar, signalVar], body);
    }

    private void EmitLoopIterationBody(
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget breakLabel,
        LabelTarget continueLabel,
        bool hasConditionCheck)
    {
        body.Add(LinqExpression.Call(
            CheckExecutionConstraintsMethod,
            _constraintStateParam,
            LinqExpression.Property(_configParam, nameof(AlderConfig.Constraints)),
            _ctParam));
        body.Add(LinqExpression.Call(
            CheckLoopIterationConstraintMethod,
            _constraintStateParam,
            LinqExpression.Property(_configParam, nameof(AlderConfig.Constraints))));
        body.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(EmitScopedStatements(statements, includeConstraintChecks: false))));
        body.Add(BuildLoopSignalDispatch(resultVar, signalVar, breakLabel, continueLabel));
        if (!hasConditionCheck)
            body.Add(LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))));
    }

    private static LinqExpression BuildLoopSignalDispatch(
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget breakLabel,
        LabelTarget continueLabel)
    {
        var kindExpr = LinqExpression.Property(signalVar, ControlFlowSignalKindProperty);
        return LinqExpression.IfThen(
            LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
            LinqExpression.Block(
                LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                LinqExpression.IfThen(
                    LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                    LinqExpression.Block(
                        LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Break(breakLabel, resultVar))),
                LinqExpression.IfThen(
                    LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Continue)),
                    LinqExpression.Block(
                        LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Goto(continueLabel))),
                LinqExpression.Break(breakLabel, resultVar)));
    }

    private LinqExpression EmitForeachIteration(
        string variableName,
        ParameterExpression currentValue,
        ImmutableArray<BoundExpr> statements,
        Type elementType)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "foreachPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "foreachIterResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "foreachIterSignal");
        var doneLabel = LinqExpression.Label("foreachIterDone");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Call(
                _contextParam,
                ContextDefineNewMethod,
                LinqExpression.Constant(variableName),
                currentValue,
                LinqExpression.Constant(elementType, typeof(Type)),
                LinqExpression.Constant(false))
        };
        EmitStatementListBody(
            body,
            statements,
            resultVar,
            signalVar,
            doneLabel,
            unwrapReturnSignal: false,
            includeConstraintChecks: false);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(_contextParam, previousContextVar)),
            resultVar);
    }

    private LinqExpression EmitScopedStatements(ImmutableArray<BoundExpr> statements, bool includeConstraintChecks = true)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "scopePrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "scopeResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "scopeSignal");
        var doneLabel = LinqExpression.Label("scopeDone");
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(
            body,
            statements,
            resultVar,
            signalVar,
            doneLabel,
            unwrapReturnSignal: false,
            includeConstraintChecks: includeConstraintChecks);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, _contextParam),
            LinqExpression.Assign(_contextParam, LinqExpression.Call(_contextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(_contextParam, previousContextVar)),
            resultVar);
    }

    private void EmitStatementListBody(
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        ParameterExpression resultVar,
        ParameterExpression signalVar,
        LabelTarget doneLabel,
        bool unwrapReturnSignal,
        bool includeConstraintChecks = true)
    {
        for (var i = 0; i < statements.Length; i++)
        {
            if (includeConstraintChecks)
            {
                body.Add(LinqExpression.Call(
                    CheckExecutionConstraintsMethod,
                    _constraintStateParam,
                    LinqExpression.Property(_configParam, nameof(AlderConfig.Constraints)),
                    _ctParam));
            }
            body.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(Emit(statements[i]))));
            body.Add(
                LinqExpression.IfThen(
                    LinqExpression.TypeIs(resultVar, typeof(ControlFlowSignal)),
                    LinqExpression.Block(
                        LinqExpression.Assign(signalVar, LinqExpression.TypeAs(resultVar, typeof(ControlFlowSignal))),
                        unwrapReturnSignal
                            ? LinqExpression.IfThen(
                                LinqExpression.Equal(
                                    LinqExpression.Property(signalVar, ControlFlowSignalKindProperty),
                                    LinqExpression.Constant(ControlFlowSignal.Kind.Return)),
                                LinqExpression.Assign(
                                    resultVar,
                                    LinqExpression.Property(signalVar, ControlFlowValueProperty)))
                            : LinqExpression.Empty(),
                        LinqExpression.Goto(doneLabel))));
        }
    }

}
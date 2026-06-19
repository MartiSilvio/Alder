using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Compilation;
using Alder.Interpretation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

[EmitsNode(BoundNodeKind.Block)]
internal static class BlockEmitter
{
    public static LinqExpression Emit(BoundBlockExpr node, EmissionContext ctx)
    {
        var statements = node.Statements;
        var hasLabels = statements.Any(s => s is BoundLabelExpr);

        if (hasLabels)
            return EmitWithLabels(node, ctx);

        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "prevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "blockResult");
        var doneLabel = LinqExpression.Label("blockDone");

        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(ctx, body, statements, resultVar, doneLabel);
        if (node.ReturnExpr != null)
            body.Add(LinqExpression.Assign(resultVar, ctx.EmitBoxed(node.ReturnExpr)));
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    private static LinqExpression EmitWithLabels(BoundBlockExpr block, EmissionContext ctx)
    {
        var statements = block.Statements;
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "prevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "blockResult");
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
                    ctx.ConstraintStateParam,
                    LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints)),
                    ctx.CancellationTokenParam),
                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(ctx.Emit(statements[i]))),
                LinqExpression.IfThen(
                    HasSignal(ctx),
                    LinqExpression.Block(
                        BuildBlockGotoCheck(ctx.SignalParam, resultVar, startIndexVar, loopContinue, labelIndices),
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
            LinqExpression.Assign(startIndexVar, LinqExpression.Constant(0)),
            LinqExpression.Loop(
                LinqExpression.Block(typeof(void), loopBody),
                loopBreak,
                loopContinue)
        };

        if (block.ReturnExpr != null)
            outerBody.Add(LinqExpression.Assign(resultVar, ctx.EmitBoxed(block.ReturnExpr)));
        outerBody.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, startIndexVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(outerBody),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    private static LinqExpression BuildBlockGotoCheck(
        ParameterExpression signalParam,
        ParameterExpression resultVar,
        ParameterExpression startIndexVar,
        LabelTarget loopContinue,
        Dictionary<string, int> labelIndices)
    {
        LinqExpression check = LinqExpression.Empty();
        var kindExpr = LinqExpression.Property(signalParam, ControlFlowSignalKindProperty);
        var valueExpr = LinqExpression.Property(signalParam, ControlFlowValueProperty);

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
                    LinqExpression.Assign(signalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                    LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                    LinqExpression.Continue(loopContinue)));
        }

        return check;
    }

    internal static LinqExpression EmitScopedStatements(EmissionContext ctx, ImmutableArray<BoundExpr> statements)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "scopePrevCtx");
        var doneLabel = LinqExpression.Label("scopeDone");
        var emittedStatements = new List<LinqExpression>();

        EmitStatementListInto(ctx, emittedStatements, statements, doneLabel);

        var resultType = emittedStatements.Count > 0
            ? GetLastStatementType(emittedStatements)
            : typeof(object);
        var resultVar = LinqExpression.Variable(resultType, "scopeResult");

        var body = new List<LinqExpression> { LinqExpression.Assign(resultVar, LinqExpression.Default(resultType)) };
        WrapLastStatementAssignment(emittedStatements, resultVar);
        body.AddRange(emittedStatements);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            resultType,
            [previousContextVar, resultVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    private static LinqExpression EmitScopedStatementsUnchecked(EmissionContext ctx, ImmutableArray<BoundExpr> statements)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "scopePrevCtx");
        var doneLabel = LinqExpression.Label("scopeDone");
        var emittedStatements = new List<LinqExpression>();

        EmitStatementListUncheckedInto(ctx, emittedStatements, statements, doneLabel);

        var resultType = emittedStatements.Count > 0
            ? GetLastStatementType(emittedStatements)
            : typeof(object);
        var resultVar = LinqExpression.Variable(resultType, "scopeResult");

        var body = new List<LinqExpression> { LinqExpression.Assign(resultVar, LinqExpression.Default(resultType)) };
        WrapLastStatementAssignment(emittedStatements, resultVar);
        body.AddRange(emittedStatements);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            resultType,
            [previousContextVar, resultVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    internal static LinqExpression EmitStatementSequence(EmissionContext ctx, ImmutableArray<BoundExpr> statements)
    {
        var resultVar = LinqExpression.Variable(typeof(object), "seqResult");
        var doneLabel = LinqExpression.Label("seqDone");

        var emittedStatements = new List<LinqExpression> { LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))) };
        EmitStatementListInto(ctx, emittedStatements, statements, doneLabel);
        WrapLastStatementAssignment(emittedStatements, resultVar);
        emittedStatements.Add(LinqExpression.Label(doneLabel));
        emittedStatements.Add(resultVar);

        return LinqExpression.Block(typeof(object), [resultVar], emittedStatements);
    }

    internal static void EmitStatementListBody(
        EmissionContext ctx,
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        ParameterExpression resultVar,
        LabelTarget doneLabel)
    {
        var emitted = new List<LinqExpression>();
        EmitStatementListInto(ctx, emitted, statements, doneLabel);
        WrapLastStatementAssignment(emitted, resultVar);
        body.AddRange(emitted);
    }

    private static void EmitStatementListInto(
        EmissionContext ctx,
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        LabelTarget doneLabel)
    {
        foreach (var t in statements)
        {
            body.Add(LinqExpression.Call(
                CheckExecutionConstraintsMethod,
                ctx.ConstraintStateParam,
                LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints)),
                ctx.CancellationTokenParam));

            body.Add(ctx.Emit(t));
            body.Add(LinqExpression.IfThen(HasSignal(ctx), LinqExpression.Goto(doneLabel)));
        }
    }

    private static void EmitStatementListUncheckedInto(
        EmissionContext ctx,
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        LabelTarget doneLabel)
    {
        for (var i = 0; i < statements.Length; i++)
        {
            body.Add(ctx.Emit(statements[i]));
            body.Add(LinqExpression.IfThen(HasSignal(ctx), LinqExpression.Goto(doneLabel)));
        }
    }

    private static Type GetLastStatementType(List<LinqExpression> emitted)
    {
        var lastIdx = emitted.Count - 2;
        var lastType = lastIdx >= 0 ? emitted[lastIdx].Type : typeof(object);
        return lastType == typeof(void) ? typeof(object) : lastType;
    }

    private static void WrapLastStatementAssignment(List<LinqExpression> emitted, ParameterExpression resultVar)
    {
        if (emitted.Count < 2) return;
        var lastStmtIdx = emitted.Count - 2;
        var lastEmitted = emitted[lastStmtIdx];
        emitted[lastStmtIdx] = LinqExpression.Assign(resultVar,
            lastEmitted.Type == resultVar.Type
                ? lastEmitted
                : LinqExpression.Convert(lastEmitted, resultVar.Type));
    }

    internal static void EmitLoopIterationBody(
        EmissionContext ctx,
        List<LinqExpression> body,
        ImmutableArray<BoundExpr> statements,
        ParameterExpression resultVar,
        LabelTarget breakLabel,
        LabelTarget continueLabel,
        bool hasConditionCheck)
    {
        body.Add(LinqExpression.Call(
            CheckExecutionConstraintsMethod,
            ctx.ConstraintStateParam,
            LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints)),
            ctx.CancellationTokenParam));
        body.Add(LinqExpression.Call(
            CheckLoopIterationConstraintMethod,
            ctx.ConstraintStateParam,
            LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints))));
        body.Add(EmitScopedStatementsUnchecked(ctx, statements));
        body.Add(BuildLoopSignalDispatch(ctx, resultVar, breakLabel, continueLabel));
        if (!hasConditionCheck)
            body.Add(LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))));
    }

    internal static LinqExpression BuildLoopSignalDispatch(
        EmissionContext ctx,
        ParameterExpression resultVar,
        LabelTarget breakLabel,
        LabelTarget continueLabel)
    {
        var kindExpr = LinqExpression.Property(ctx.SignalParam, ControlFlowSignalKindProperty);
        return LinqExpression.IfThen(
            HasSignal(ctx),
            LinqExpression.Block(
                LinqExpression.IfThen(
                    LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Break)),
                    LinqExpression.Block(
                        LinqExpression.Assign(ctx.SignalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                        LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Break(breakLabel, resultVar))),
                LinqExpression.IfThen(
                    LinqExpression.Equal(kindExpr, LinqExpression.Constant(ControlFlowSignal.Kind.Continue)),
                    LinqExpression.Block(
                        LinqExpression.Assign(ctx.SignalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal))),
                        LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
                        LinqExpression.Goto(continueLabel))),
                LinqExpression.Break(breakLabel, resultVar)));
    }

    internal static LinqExpression EmitForeachIteration(
        EmissionContext ctx,
        string variableName,
        ParameterExpression currentValue,
        ImmutableArray<BoundExpr> statements,
        Type elementType,
        Type? sourceElementType)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "foreachPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "foreachIterResult");
        var doneLabel = LinqExpression.Label("foreachIterDone");
        LinqExpression iterationValue = !TypeHelpers.RequiresIterationCast(elementType, sourceElementType)
            ? currentValue
            : LinqExpression.Call(
                ExplicitCastMethod,
                currentValue,
                LinqExpression.Constant(elementType, typeof(Type)),
                LinqExpression.Constant(sourceElementType, typeof(Type)),
                LinqExpression.Constant(ctx.IsChecked));
        var body = new List<LinqExpression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Call(
                ctx.ContextParam,
                ContextDefineNewMethod,
                LinqExpression.Constant(variableName),
                iterationValue,
                LinqExpression.Constant(elementType, typeof(Type)),
                LinqExpression.Constant(true))
        };
        var emitted = new List<LinqExpression>();
        EmitStatementListUncheckedInto(ctx, emitted, statements, doneLabel);
        WrapLastStatementAssignment(emitted, resultVar);
        body.AddRange(emitted);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    private static LinqExpression HasSignal(EmissionContext ctx) =>
        LinqExpression.NotEqual(ctx.SignalParam, LinqExpression.Constant(null, typeof(ControlFlowSignal)));
}

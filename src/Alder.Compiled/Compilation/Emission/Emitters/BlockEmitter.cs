using System.Collections.Immutable;
using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Interpretation;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class BlockEmitter : INodeEmitter<BoundBlockExpr>
{
    public Expression Emit(BoundBlockExpr node, EmissionContext ctx)
    {
        var statements = node.Statements;
        var hasLabels = statements.Any(s => s is BoundLabelExpr);

        if (hasLabels)
            return EmitWithLabels(node, ctx);

        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "prevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "blockResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "blockSignal");
        var doneLabel = LinqExpression.Label("blockDone");

        var body = new List<Expression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(ctx, body, statements, resultVar, signalVar, doneLabel, unwrapReturnSignal: false);
        if (node.ReturnExpr != null)
            body.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(ctx.Emit(node.ReturnExpr))));
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    private static Expression EmitWithLabels(BoundBlockExpr block, EmissionContext ctx)
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

        var loopBody = new List<Expression>();

        for (var i = 0; i < statements.Length; i++)
        {
            var stmtBody = new List<Expression>
            {
                LinqExpression.Call(
                    CheckExecutionConstraintsMethod,
                    ctx.ConstraintStateParam,
                    LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints)),
                    ctx.CancellationTokenParam),
                LinqExpression.Assign(resultVar, EmitHelpers.AsObject(ctx.Emit(statements[i]))),
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

        var outerBody = new List<Expression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Assign(startIndexVar, LinqExpression.Constant(0)),
            LinqExpression.Loop(
                LinqExpression.Block(typeof(void), loopBody),
                loopBreak,
                loopContinue)
        };

        if (block.ReturnExpr != null)
            outerBody.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(ctx.Emit(block.ReturnExpr))));
        outerBody.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar, startIndexVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(outerBody),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    private static Expression BuildBlockGotoCheck(
        ParameterExpression signalVar,
        ParameterExpression resultVar,
        ParameterExpression startIndexVar,
        LabelTarget loopContinue,
        Dictionary<string, int> labelIndices)
    {
        Expression check = LinqExpression.Empty();
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

    internal static Expression EmitScopedStatements(EmissionContext ctx, ImmutableArray<BoundExpr> statements, bool includeConstraintChecks = true)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "scopePrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "scopeResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "scopeSignal");
        var doneLabel = LinqExpression.Label("scopeDone");
        var body = new List<Expression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(ctx, body, statements, resultVar, signalVar, doneLabel,
            unwrapReturnSignal: false, includeConstraintChecks: includeConstraintChecks);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }

    internal static Expression EmitStatementSequence(EmissionContext ctx, ImmutableArray<BoundExpr> statements)
    {
        var resultVar = LinqExpression.Variable(typeof(object), "seqResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "seqSignal");
        var doneLabel = LinqExpression.Label("seqDone");
        var body = new List<Expression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object)))
        };

        EmitStatementListBody(ctx, body, statements, resultVar, signalVar, doneLabel, unwrapReturnSignal: false);
        body.Add(LinqExpression.Label(doneLabel));
        body.Add(resultVar);

        return LinqExpression.Block(typeof(object), [resultVar, signalVar], body);
    }

    internal static void EmitStatementListBody(
        EmissionContext ctx,
        List<Expression> body,
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
                    ctx.ConstraintStateParam,
                    LinqExpression.Property(ctx.ConfigParam, nameof(AlderConfig.Constraints)),
                    ctx.CancellationTokenParam));
            }
            body.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(ctx.Emit(statements[i]))));
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

    internal static void EmitLoopIterationBody(
        EmissionContext ctx,
        List<Expression> body,
        ImmutableArray<BoundExpr> statements,
        ParameterExpression resultVar,
        ParameterExpression signalVar,
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
        body.Add(LinqExpression.Assign(resultVar, EmitHelpers.AsObject(EmitScopedStatements(ctx, statements, includeConstraintChecks: false))));
        body.Add(BuildLoopSignalDispatch(resultVar, signalVar, breakLabel, continueLabel));
        if (!hasConditionCheck)
            body.Add(LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))));
    }

    internal static Expression BuildLoopSignalDispatch(
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

    internal static Expression EmitForeachIteration(
        EmissionContext ctx,
        string variableName,
        ParameterExpression currentValue,
        ImmutableArray<BoundExpr> statements,
        Type elementType)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "foreachPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "foreachIterResult");
        var signalVar = LinqExpression.Variable(typeof(ControlFlowSignal), "foreachIterSignal");
        var doneLabel = LinqExpression.Label("foreachIterDone");
        var body = new List<Expression>
        {
            LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))),
            LinqExpression.Call(
                ctx.ContextParam,
                ContextDefineNewMethod,
                LinqExpression.Constant(variableName),
                currentValue,
                LinqExpression.Constant(elementType, typeof(Type)),
                LinqExpression.Constant(false))
        };
        EmitStatementListBody(ctx, body, statements, resultVar, signalVar, doneLabel,
            unwrapReturnSignal: false, includeConstraintChecks: false);
        body.Add(LinqExpression.Label(doneLabel));

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar, signalVar],
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod)),
            LinqExpression.TryFinally(
                LinqExpression.Block(body),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)),
            resultVar);
    }
}

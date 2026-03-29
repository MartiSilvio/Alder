using System.Linq.Expressions;
using Alder.Binding;
using Alder.Binding.BoundNodes;
using Alder.Runtime;
using static Alder.Compiled.Compilation.BoundRuntimeMethodCache;

namespace Alder.Compiled.Compilation.Emission.Emitters;

internal sealed class TryCatchEmitter : INodeEmitter<BoundTryCatchFinallyExpr>
{
    public LinqExpression Emit(BoundTryCatchFinallyExpr node, EmissionContext ctx)
    {
        var tryBody = BlockEmitter.EmitStatementSequence(ctx, node.TryBody);
        var catchBlocks = new List<CatchBlock>(node.CatchClauses.Length);

        for (var i = 0; i < node.CatchClauses.Length; i++)
        {
            var catchClause = node.CatchClauses[i];
            var exParam = LinqExpression.Parameter(typeof(Exception), $"catchEx{i}");
            var catchBody = EmitCatchClauseBody(catchClause, exParam, ctx);
            var filter = BuildCatchFilter(catchClause, exParam, ctx);
            catchBlocks.Add(LinqExpression.MakeCatchBlock(typeof(Exception), exParam, catchBody, filter));
        }

        LinqExpression? finallyBody = null;
        if (!node.FinallyBody.IsDefaultOrEmpty)
        {
            var statements = new List<LinqExpression>(node.FinallyBody.Length);
            for (var i = 0; i < node.FinallyBody.Length; i++)
                statements.Add(ctx.Emit(node.FinallyBody[i]));
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

    private static LinqExpression EmitCatchClauseBody(BoundCatchClause catchClause, ParameterExpression exParam, EmissionContext ctx)
    {
        var previousContextVar = LinqExpression.Variable(typeof(AlderContext), "catchPrevCtx");
        var resultVar = LinqExpression.Variable(typeof(object), "catchResult");
        var doneLabel = LinqExpression.Label("catchDone");
        var bodyStatements = new List<LinqExpression>();

        var previousDepth = ctx.CatchDepth;
        ctx.CatchDepth = previousDepth + 1;
        try
        {
            bodyStatements.Add(LinqExpression.Assign(resultVar, LinqExpression.Constant(null, typeof(object))));
            BlockEmitter.EmitStatementListBody(ctx, bodyStatements, catchClause.Body, resultVar, doneLabel);
            bodyStatements.Add(LinqExpression.Label(doneLabel));
        }
        finally
        {
            ctx.CatchDepth = previousDepth;
        }

        var scopedStatements = new List<LinqExpression>
        {
            LinqExpression.Assign(previousContextVar, ctx.ContextParam),
            LinqExpression.Assign(ctx.ContextParam, LinqExpression.Call(ctx.ContextParam, ContextCreateChildMethod))
        };

        if (catchClause.VariableName != null)
        {
            scopedStatements.Add(
                LinqExpression.Call(
                    ctx.ContextParam,
                    ContextDefineNewMethod,
                    LinqExpression.Constant(catchClause.VariableName),
                    LinqExpression.Convert(exParam, typeof(object)),
                    LinqExpression.Call(exParam, typeof(object).GetMethod(nameof(GetType))!),
                    LinqExpression.Constant(false)));
        }

        scopedStatements.Add(
            LinqExpression.TryFinally(
                LinqExpression.Block(bodyStatements),
                LinqExpression.Assign(ctx.ContextParam, previousContextVar)));
        scopedStatements.Add(resultVar);

        return LinqExpression.Block(
            typeof(object),
            [previousContextVar, resultVar],
            scopedStatements);
    }

    private static LinqExpression? BuildCatchFilter(BoundCatchClause catchClause, ParameterExpression exParam, EmissionContext ctx)
    {
        LinqExpression? typeFilter = null;
        if (catchClause.ExceptionTypeName != null)
        {
            var resolvedType = ctx.ResolveTypeByName(catchClause.ExceptionTypeName);
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
                ctx.ContextParam,
                ctx.ConfigParam,
                ctx.CancellationTokenParam);
        }

        if (typeFilter == null)
            return whenFilter;
        if (whenFilter == null)
            return typeFilter;
        return LinqExpression.AndAlso(typeFilter, whenFilter);
    }
}

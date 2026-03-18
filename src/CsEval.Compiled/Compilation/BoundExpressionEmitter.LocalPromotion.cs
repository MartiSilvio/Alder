using System.Linq.Expressions;
using CsEval.Binding;
using CsEval.Binding.BoundNodes;

namespace CsEval.Compiled.Compilation;

internal sealed partial class BoundExpressionEmitter
{
    private Dictionary<int, PromotedLocal>? _promotedLocals;

    private sealed record PromotedLocal(string Name, ParameterExpression Variable, Type VariableType);

    private bool TryGetPromoted(int? localId, out PromotedLocal promoted)
    {
        if (localId is { } id && _promotedLocals != null && _promotedLocals.TryGetValue(id, out promoted!))
            return true;
        promoted = null!;
        return false;
    }

    private static Dictionary<int, PromotedLocal> BuildLocalPromotionPlan(BoundExpr root)
    {
        var result = new Dictionary<int, PromotedLocal>();
        var hasLambda = false;

        WalkForPromotion(root, result, ref hasLambda);

        if (hasLambda)
            return new Dictionary<int, PromotedLocal>();

        // If the same variable name appears in multiple declarations (different LocalIds),
        // the runtime would reject it via DefineNew's duplicate check. We must let the
        // runtime path handle that, so exclude all declarations with conflicting names.
        var nameCount = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var promoted in result.Values)
        {
            nameCount.TryGetValue(promoted.Name, out var count);
            nameCount[promoted.Name] = count + 1;
        }

        var idsToRemove = new List<int>();
        foreach (var (id, promoted) in result)
        {
            if (nameCount[promoted.Name] > 1)
                idsToRemove.Add(id);
        }

        foreach (var id in idsToRemove)
            result.Remove(id);

        return result;
    }

    private static void WalkForPromotion(BoundExpr expr, Dictionary<int, PromotedLocal> result, ref bool hasLambda)
    {
        while (true)
        {
            if (hasLambda) return;

            if (expr is BoundLambdaExpr)
            {
                hasLambda = true;
                return;
            }

            if (expr is BoundVariableDeclExpr { IsConst: false } decl
                && decl.StaticType != typeof(object)
                && decl.LocalId is { } declLocalId)
            {
                var variableType = decl.DeclaredType ?? decl.StaticType;
                result[declLocalId] = new PromotedLocal(
                    decl.Name,
                    LinqExpression.Variable(typeof(object), $"local_{decl.Name}"),
                    variableType);
            }

            // Tail-call optimize single-child nodes to avoid stack overflow on deep chains
            switch (expr)
            {
                case BoundMemberAccessExpr ma: expr = ma.Target; continue;
                case BoundUnaryExpr u: expr = u.Operand; continue;
                case BoundCastExpr c: expr = c.Expression; continue;
                case BoundAsExpr a: expr = a.Expression; continue;
                case BoundIsPatternExpr ip: expr = ip.Expression; continue;
                case BoundCheckedExpr ch: expr = ch.Expression; continue;
                case BoundSpreadExpr sp: expr = sp.Expression; continue;
                case BoundThrowExpr th: expr = th.Expression; continue;
                case BoundIndexFromEndExpr ife: expr = ife.Operand; continue;
                case BoundNamedArgumentExpr na: expr = na.Value; continue;
                case BoundTypedArrayCreationExpr tac: expr = tac.Size; continue;
                case BoundVariableDeclExpr vd: expr = vd.Initializer; continue;
                case BoundAssignExpr assign: expr = assign.Value; continue;
                case BoundCompoundAssignExpr ca: expr = ca.Value; continue;
                case BoundNullCoalesceAssignExpr nca: expr = nca.Value; continue;
                case BoundDeconstructionExpr dec: expr = dec.ValueExpression; continue;
                case BoundReturnExpr { Value: { } retVal }: expr = retVal; continue;
                default:
                    WalkChildren(expr, result, ref hasLambda);
                    return;
            }
        }
    }

    // Handles multi-child nodes only. Single-child nodes are tail-call optimized in WalkForPromotion.
    private static void WalkChildren(BoundExpr expr, Dictionary<int, PromotedLocal> result, ref bool hasLambda)
    {
        switch (expr)
        {
            case BoundBlockExpr block:
                foreach (var s in block.Statements) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                if (block.ReturnExpr != null) WalkForPromotion(block.ReturnExpr, result, ref hasLambda);
                break;
            case BoundForExpr f:
                foreach (var i in f.Initializers) { WalkForPromotion(i, result, ref hasLambda); if (hasLambda) return; }
                if (f.Condition != null) WalkForPromotion(f.Condition, result, ref hasLambda);
                foreach (var i in f.Increments) { WalkForPromotion(i, result, ref hasLambda); if (hasLambda) return; }
                foreach (var s in f.Body) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundWhileExpr w:
                WalkForPromotion(w.Condition, result, ref hasLambda);
                foreach (var s in w.Body) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundDoWhileExpr d:
                foreach (var s in d.Body) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                WalkForPromotion(d.Condition, result, ref hasLambda);
                break;
            case BoundForEachExpr fe:
                WalkForPromotion(fe.Collection, result, ref hasLambda);
                foreach (var s in fe.Body) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundIfStatementExpr ifs:
                WalkForPromotion(ifs.Condition, result, ref hasLambda);
                foreach (var s in ifs.ThenStatements) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                foreach (var s in ifs.ElseStatements) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundConditionalExpr c:
                WalkForPromotion(c.Condition, result, ref hasLambda);
                WalkForPromotion(c.ThenBranch, result, ref hasLambda);
                WalkForPromotion(c.ElseBranch, result, ref hasLambda);
                break;
            case BoundBinaryExpr b:
                WalkForPromotion(b.Left, result, ref hasLambda);
                WalkForPromotion(b.Right, result, ref hasLambda);
                break;
            case BoundLogicalExpr l:
                WalkForPromotion(l.Left, result, ref hasLambda);
                WalkForPromotion(l.Right, result, ref hasLambda);
                break;
            case BoundNullCoalesceExpr nc:
                WalkForPromotion(nc.Left, result, ref hasLambda);
                WalkForPromotion(nc.Right, result, ref hasLambda);
                break;
            case BoundCallExpr call:
                WalkForPromotion(call.Callee, result, ref hasLambda);
                foreach (var a in call.Arguments) { WalkForPromotion(a, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundInvokeExpr invoke:
                WalkForPromotion(invoke.Callee, result, ref hasLambda);
                foreach (var a in invoke.Arguments) { WalkForPromotion(a, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundMemberAssignExpr ma:
                WalkForPromotion(ma.Target, result, ref hasLambda);
                WalkForPromotion(ma.Value, result, ref hasLambda);
                break;
            case BoundMemberCompoundAssignExpr mca:
                WalkForPromotion(mca.Target, result, ref hasLambda);
                WalkForPromotion(mca.Value, result, ref hasLambda);
                break;
            case BoundMemberIncrementExpr mi:
                WalkForPromotion(mi.Target, result, ref hasLambda);
                break;
            case BoundMemberNullCoalesceAssignExpr mnca:
                WalkForPromotion(mnca.Target, result, ref hasLambda);
                WalkForPromotion(mnca.Value, result, ref hasLambda);
                break;
            case BoundIndexAccessExpr ia:
                WalkForPromotion(ia.Target, result, ref hasLambda);
                WalkForPromotion(ia.Index, result, ref hasLambda);
                break;
            case BoundIndexAssignExpr ia:
                WalkForPromotion(ia.Target, result, ref hasLambda);
                WalkForPromotion(ia.Index, result, ref hasLambda);
                WalkForPromotion(ia.Value, result, ref hasLambda);
                break;
            case BoundIndexCompoundAssignExpr ica:
                WalkForPromotion(ica.Target, result, ref hasLambda);
                WalkForPromotion(ica.Index, result, ref hasLambda);
                WalkForPromotion(ica.Value, result, ref hasLambda);
                break;
            case BoundIndexIncrementExpr ii:
                WalkForPromotion(ii.Target, result, ref hasLambda);
                WalkForPromotion(ii.Index, result, ref hasLambda);
                break;
            case BoundIndexNullCoalesceAssignExpr inca:
                WalkForPromotion(inca.Target, result, ref hasLambda);
                WalkForPromotion(inca.Index, result, ref hasLambda);
                WalkForPromotion(inca.Value, result, ref hasLambda);
                break;
            case BoundMultiDimIndexAccessExpr mdia:
                WalkForPromotion(mdia.Target, result, ref hasLambda);
                foreach (var i in mdia.Indices) { WalkForPromotion(i, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundMultiDimIndexAssignExpr mdia:
                WalkForPromotion(mdia.Target, result, ref hasLambda);
                foreach (var i in mdia.Indices) { WalkForPromotion(i, result, ref hasLambda); if (hasLambda) return; }
                WalkForPromotion(mdia.Value, result, ref hasLambda);
                break;
            case BoundSliceExpr s:
                WalkForPromotion(s.Target, result, ref hasLambda);
                if (s.Start != null) WalkForPromotion(s.Start, result, ref hasLambda);
                if (s.End != null) WalkForPromotion(s.End, result, ref hasLambda);
                if (s.Step != null) WalkForPromotion(s.Step, result, ref hasLambda);
                break;
            case BoundArrayLiteralExpr al:
                foreach (var e in al.Elements) { WalkForPromotion(e, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundTypedArrayLiteralExpr tal:
                foreach (var e in tal.Elements) { WalkForPromotion(e, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundTypedArrayCreationExpr tac:
                WalkForPromotion(tac.Size, result, ref hasLambda);
                break;
            case BoundMultiDimTypedArrayCreationExpr mdtac:
                foreach (var s in mdtac.Sizes) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundMultiDimArrayInitExpr mdai:
                if (mdai.ExplicitSizes != null)
                    foreach (var s in mdai.ExplicitSizes.Value) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                foreach (var v in mdai.FlatValues) { WalkForPromotion(v, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundTupleExpr t:
                foreach (var e in t.Elements) { WalkForPromotion(e, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundChainedComparisonExpr cc:
                foreach (var o in cc.Operands) { WalkForPromotion(o, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundPipelineExpr p:
                WalkForPromotion(p.Left, result, ref hasLambda);
                WalkForPromotion(p.Right, result, ref hasLambda);
                break;
            case BoundRangeExpr r:
                WalkForPromotion(r.Start, result, ref hasLambda);
                WalkForPromotion(r.End, result, ref hasLambda);
                break;
            case BoundSpreadExpr sp:
                WalkForPromotion(sp.Expression, result, ref hasLambda);
                break;
            case BoundThrowExpr th:
                WalkForPromotion(th.Expression, result, ref hasLambda);
                break;
            case BoundIndexFromEndExpr ife:
                WalkForPromotion(ife.Operand, result, ref hasLambda);
                break;
            case BoundNamedArgumentExpr na:
                WalkForPromotion(na.Value, result, ref hasLambda);
                break;
            case BoundDeconstructionExpr dec:
                WalkForPromotion(dec.ValueExpression, result, ref hasLambda);
                break;
            case BoundObjectCreationExpr oc:
                foreach (var a in oc.Arguments) { WalkForPromotion(a, result, ref hasLambda); if (hasLambda) return; }
                foreach (var e in oc.InitializerEntries)
                {
                    WalkForPromotion(e.Value, result, ref hasLambda); if (hasLambda) return;
                    if (e.IndexerKey != null) { WalkForPromotion(e.IndexerKey, result, ref hasLambda); if (hasLambda) return; }
                }
                break;
            case BoundObjectLiteralExpr ol:
                foreach (var p in ol.Properties) { WalkForPromotion(p.Value, result, ref hasLambda); if (hasLambda) return; }
                break;
            case BoundInterpolatedStringExpr interp:
                foreach (var p in interp.Parts)
                    if (p is BoundInterpolatedExpressionPart ep)
                    {
                        WalkForPromotion(ep.Expression, result, ref hasLambda); if (hasLambda) return;
                    }
                break;
            case BoundLockStatementExpr lk:
                WalkForPromotion(lk.LockObject, result, ref hasLambda);
                WalkForPromotion(lk.Body, result, ref hasLambda);
                break;
            case BoundUsingStatementExpr us:
                WalkForPromotion(us.Resource, result, ref hasLambda);
                WalkForPromotion(us.Body, result, ref hasLambda);
                break;
            case BoundSwitchStatementExpr ss:
                WalkForPromotion(ss.Expression, result, ref hasLambda);
                foreach (var c in ss.Cases)
                {
                    if (c.WhenGuard != null) { WalkForPromotion(c.WhenGuard, result, ref hasLambda); if (hasLambda) return; }
                    foreach (var s in c.Statements) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                }
                break;
            case BoundSwitchExpressionExpr se:
                WalkForPromotion(se.Expression, result, ref hasLambda);
                foreach (var a in se.Arms)
                {
                    if (a.WhenGuard != null) { WalkForPromotion(a.WhenGuard, result, ref hasLambda); if (hasLambda) return; }
                    WalkForPromotion(a.Value, result, ref hasLambda); if (hasLambda) return;
                }
                break;
            case BoundGotoCaseExpr gc:
                WalkForPromotion(gc.Value, result, ref hasLambda);
                break;
            case BoundTryCatchFinallyExpr tcf:
                foreach (var s in tcf.TryBody) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                foreach (var c in tcf.CatchClauses)
                {
                    if (c.WhenGuard != null) { WalkForPromotion(c.WhenGuard, result, ref hasLambda); if (hasLambda) return; }
                    foreach (var s in c.Body) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                }
                foreach (var s in tcf.FinallyBody) { WalkForPromotion(s, result, ref hasLambda); if (hasLambda) return; }
                break;
        }
    }
}

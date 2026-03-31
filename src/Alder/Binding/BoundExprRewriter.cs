using System.Collections.Immutable;
using Alder.Binding.BoundNodes;
using Alder.Pipeline;

namespace Alder.Binding;

internal abstract class BoundExprRewriter : BoundExprVisitor<BoundExpr>, IBoundTreePass
{
    public BoundExpr Rewrite(BoundExpr tree) => Visit(tree);

    BoundExpr IBoundTreePass.Execute(BoundExpr tree, PipelineContext context) => Rewrite(tree);

    protected override BoundExpr DefaultVisit(BoundExpr node) => node;

    private BoundExpr CopyMetadata(BoundExpr original, BoundExpr rewritten)
    {
        if (ReferenceEquals(original, rewritten)) return rewritten;
        rewritten.Span = original.Span;
        if (original.HasErrors) rewritten.HasErrors = true;
        if (original.Diagnostic != null) rewritten.Diagnostic = original.Diagnostic;
        return rewritten;
    }

    protected override BoundExpr VisitLiteral(BoundLiteralExpr node) => node;

    protected override BoundExpr VisitIdentifier(BoundIdentifierExpr node) => node;

    protected override BoundExpr VisitInterpolatedString(BoundInterpolatedStringExpr node)
    {
        var changed = false;
        var parts = ImmutableArray.CreateBuilder<BoundInterpolatedPart>(node.Parts.Length);
        foreach (var part in node.Parts)
        {
            if (part is BoundInterpolatedExpressionPart ep)
            {
                var newExpr = Visit(ep.Expression);
                if (!ReferenceEquals(newExpr, ep.Expression))
                {
                    parts.Add(new BoundInterpolatedExpressionPart(newExpr, ep.AlignmentSpecifier, ep.FormatSpecifier));
                    changed = true;
                    continue;
                }
            }
            parts.Add(part);
        }
        if (!changed) return node;
        return CopyMetadata(node, node with { Parts = parts.MoveToImmutable() });
    }

    protected override BoundExpr VisitLabel(BoundLabelExpr node) => node;

    protected override BoundExpr VisitUnary(BoundUnaryExpr node)
    {
        var operand = Visit(node.Operand);
        if (ReferenceEquals(operand, node.Operand)) return node;
        return CopyMetadata(node, node with { Operand = operand });
    }

    protected override BoundExpr VisitBinary(BoundBinaryExpr node)
    {
        // Iterative flattening for left-deep binary chains.
        // Each rebuilt node goes through RevisitBinary so subclasses can fold intermediates.
        var chain = new List<BoundBinaryExpr>();
        BoundExpr leftmost = node;
        while (leftmost is BoundBinaryExpr b)
        {
            chain.Add(b);
            leftmost = b.Left;
        }

        var current = Visit(leftmost);

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var original = chain[i];
            var newRight = Visit(original.Right);

            if (!ReferenceEquals(current, original.Left) || !ReferenceEquals(newRight, original.Right))
            {
                var rewritten = original with { Left = current, Right = newRight };
                current = RevisitBinary(CopyMetadata(original, rewritten));
            }
            else
            {
                current = RevisitBinary(original);
            }
        }

        return current;
    }

    protected virtual BoundExpr RevisitBinary(BoundExpr node) => node;

    protected override BoundExpr VisitLogical(BoundLogicalExpr node)
    {
        var chain = new List<BoundLogicalExpr>();
        BoundExpr leftmost = node;
        while (leftmost is BoundLogicalExpr l)
        {
            chain.Add(l);
            leftmost = l.Left;
        }

        var current = Visit(leftmost);

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var original = chain[i];
            var newRight = Visit(original.Right);

            if (!ReferenceEquals(current, original.Left) || !ReferenceEquals(newRight, original.Right))
            {
                var rewritten = original with { Left = current, Right = newRight };
                current = RevisitLogical(CopyMetadata(original, rewritten));
            }
            else
            {
                current = RevisitLogical(original);
            }
        }

        return current;
    }

    protected virtual BoundExpr RevisitLogical(BoundExpr node) => node;

    protected override BoundExpr VisitChainedComparison(BoundChainedComparisonExpr node)
    {
        var operands = RewriteImmutableArray(node.Operands, out var changed);
        if (!changed) return node;
        return CopyMetadata(node, node with { Operands = operands });
    }

    protected override BoundExpr VisitIncrementDecrement(BoundIncrementDecrementExpr node) => node;

    protected override BoundExpr VisitConditional(BoundConditionalExpr node)
    {
        var condition = Visit(node.Condition);
        var then = Visit(node.ThenBranch);
        var @else = Visit(node.ElseBranch);
        if (ReferenceEquals(condition, node.Condition) &&
            ReferenceEquals(then, node.ThenBranch) &&
            ReferenceEquals(@else, node.ElseBranch))
            return node;
        return CopyMetadata(node, node with { Condition = condition, ThenBranch = then, ElseBranch = @else });
    }

    protected override BoundExpr VisitNullCoalesce(BoundNullCoalesceExpr node)
    {
        var chain = new List<BoundNullCoalesceExpr>();
        BoundExpr leftmost = node;
        while (leftmost is BoundNullCoalesceExpr nc)
        {
            chain.Add(nc);
            leftmost = nc.Left;
        }

        var current = Visit(leftmost);

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            var original = chain[i];
            var newRight = Visit(original.Right);

            if (!ReferenceEquals(current, original.Left) || !ReferenceEquals(newRight, original.Right))
            {
                var rewritten = original with { Left = current, Right = newRight };
                current = CopyMetadata(original, rewritten);
            }
            else
            {
                current = original;
            }
        }

        return current;
    }

    protected override BoundExpr VisitIsPattern(BoundIsPatternExpr node)
    {
        var expr = Visit(node.Expression);
        if (ReferenceEquals(expr, node.Expression)) return node;
        return CopyMetadata(node, node with { Expression = expr });
    }

    protected override BoundExpr VisitAs(BoundAsExpr node)
    {
        var expr = Visit(node.Expression);
        if (ReferenceEquals(expr, node.Expression)) return node;
        return CopyMetadata(node, node with { Expression = expr });
    }

    protected override BoundExpr VisitCast(BoundCastExpr node)
    {
        var expr = Visit(node.Expression);
        if (ReferenceEquals(expr, node.Expression)) return node;
        return CopyMetadata(node, node with { Expression = expr });
    }

    protected override BoundExpr VisitChecked(BoundCheckedExpr node)
    {
        var expr = Visit(node.Expression);
        if (ReferenceEquals(expr, node.Expression)) return node;
        return CopyMetadata(node, node with { Expression = expr });
    }

    protected override BoundExpr VisitPropertyAccess(BoundPropertyAccessExpr node) => VisitMemberAccess(node);
    protected override BoundExpr VisitFieldAccess(BoundFieldAccessExpr node) => VisitMemberAccess(node);
    protected override BoundExpr VisitMethodGroup(BoundMethodGroupExpr node) => VisitMemberAccess(node);
    protected override BoundExpr VisitDynamicMemberAccess(BoundDynamicMemberAccessExpr node) => VisitMemberAccess(node);

    private BoundExpr VisitMemberAccess(BoundMemberAccessBase node)
    {
        var postfix = PostfixChain.TryCollect(node);
        if (postfix != null)
            return RewritePostfixChain(postfix.Value, node);

        var target = Visit(node.Target);
        if (ReferenceEquals(target, node.Target)) return node;
        return CopyMetadata(node, RewriteMemberAccessTarget(node, target));
    }

    protected override BoundExpr VisitResolvedCall(BoundResolvedCallExpr node)
    {
        var postfix = PostfixChain.TryCollect(node);
        if (postfix != null)
            return RewritePostfixChain(postfix.Value, node);

        var callee = Visit(node.Callee);
        var args = RewriteImmutableArray(node.Arguments, out var changed);
        if (ReferenceEquals(callee, node.Callee) && !changed) return node;
        return CopyMetadata(node, node with { Callee = callee, Arguments = args });
    }

    protected override BoundExpr VisitDynamicCall(BoundDynamicCallExpr node)
    {
        var postfix = PostfixChain.TryCollect(node);
        if (postfix != null)
            return RewritePostfixChain(postfix.Value, node);

        var callee = Visit(node.Callee);
        var args = RewriteImmutableArray(node.Arguments, out var changed);
        if (ReferenceEquals(callee, node.Callee) && !changed) return node;
        return CopyMetadata(node, node with { Callee = callee, Arguments = args });
    }

    private BoundExpr RewritePostfixChain(PostfixChain.Chain chain, BoundExpr originalRoot)
    {
        var current = Visit(chain.Root);
        var anyChanged = !ReferenceEquals(current, chain.Root);

        for (var i = chain.Segments.Count - 1; i >= 0; i--)
        {
            var seg = chain.Segments[i];
            BoundMemberAccessBase originalMa = seg.MemberAccess;

            if (!ReferenceEquals(current, originalMa.Target))
            {
                current = CopyMetadata(originalMa, RewriteMemberAccessTarget(originalMa, current));
                anyChanged = true;
            }
            else
            {
                current = originalMa;
            }

            if (seg.CallOrInvoke is BoundResolvedCallExpr call)
            {
                var args = RewriteImmutableArray(call.Arguments, out var argsChanged);
                if (argsChanged || !ReferenceEquals(current, call.Callee))
                {
                    current = CopyMetadata(call, call with { Callee = current, Arguments = args });
                    anyChanged = true;
                }
                else
                {
                    current = call;
                }
            }
            else if (seg.CallOrInvoke is BoundDynamicCallExpr invoke)
            {
                var args = RewriteImmutableArray(invoke.Arguments, out var argsChanged);
                if (argsChanged || !ReferenceEquals(current, invoke.Callee))
                {
                    current = CopyMetadata(invoke, invoke with { Callee = current, Arguments = args });
                    anyChanged = true;
                }
                else
                {
                    current = invoke;
                }
            }
        }

        return anyChanged ? current : originalRoot;
    }

    protected override BoundExpr VisitResolvedIndexAccess(BoundResolvedIndexAccessExpr node)
    {
        var target = Visit(node.Target);
        var index = Visit(node.Index);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(index, node.Index)) return node;
        return CopyMetadata(node, node with { Target = target, Index = index });
    }

    protected override BoundExpr VisitDynamicIndexAccess(BoundDynamicIndexAccessExpr node)
    {
        var target = Visit(node.Target);
        var index = Visit(node.Index);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(index, node.Index)) return node;
        return CopyMetadata(node, node with { Target = target, Index = index });
    }

    protected override BoundExpr VisitResolvedMultiDimIndexAccess(BoundResolvedMultiDimIndexAccessExpr node)
    {
        var target = Visit(node.Target);
        var indices = RewriteImmutableArray(node.Indices, out var indicesChanged);
        if (ReferenceEquals(target, node.Target) && !indicesChanged) return node;
        return CopyMetadata(node, node with { Target = target, Indices = indices });
    }

    protected override BoundExpr VisitDynamicMultiDimIndexAccess(BoundDynamicMultiDimIndexAccessExpr node)
    {
        var target = Visit(node.Target);
        var indices = RewriteImmutableArray(node.Indices, out var indicesChanged);
        if (ReferenceEquals(target, node.Target) && !indicesChanged) return node;
        return CopyMetadata(node, node with { Target = target, Indices = indices });
    }

    protected override BoundExpr VisitIndexFromEnd(BoundIndexFromEndExpr node)
    {
        var operand = Visit(node.Operand);
        if (ReferenceEquals(operand, node.Operand)) return node;
        return CopyMetadata(node, node with { Operand = operand });
    }

    protected override BoundExpr VisitRange(BoundRangeExpr node)
    {
        var start = node.Start != null ? Visit(node.Start) : null;
        var end = node.End != null ? Visit(node.End) : null;
        if (ReferenceEquals(start, node.Start) && ReferenceEquals(end, node.End)) return node;
        return CopyMetadata(node, node with { Start = start, End = end });
    }

    protected override BoundExpr VisitSlice(BoundSliceExpr node)
    {
        var target = Visit(node.Target);
        var start = node.Start != null ? Visit(node.Start) : null;
        var end = node.End != null ? Visit(node.End) : null;
        var step = node.Step != null ? Visit(node.Step) : null;
        if (ReferenceEquals(target, node.Target) &&
            ReferenceEquals(start, node.Start) &&
            ReferenceEquals(end, node.End) &&
            ReferenceEquals(step, node.Step))
            return node;
        return CopyMetadata(node, node with { Target = target, Start = start, End = end, Step = step });
    }

    protected override BoundExpr VisitPipeline(BoundPipelineExpr node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);
        if (ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right)) return node;
        return CopyMetadata(node, node with { Left = left, Right = right });
    }

    protected override BoundExpr VisitNamedArgument(BoundNamedArgumentExpr node)
    {
        var value = Visit(node.Value);
        if (ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Value = value });
    }

    protected override BoundExpr VisitOutArg(BoundOutArgExpr node) => node;

    protected override BoundExpr VisitLambda(BoundLambdaExpr node) => node;

    protected override BoundExpr VisitTypedLambda(BoundTypedLambdaExpr node)
    {
        var body = Visit(node.Body);
        return ReferenceEquals(body, node.Body) ? node : node with { Body = body };
    }

    protected override BoundExpr VisitAssign(BoundAssignExpr node)
    {
        var value = Visit(node.Value);
        if (ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Value = value });
    }

    protected override BoundExpr VisitCompoundAssign(BoundCompoundAssignExpr node)
    {
        var value = Visit(node.Value);
        if (ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Value = value });
    }

    protected override BoundExpr VisitNullCoalesceAssign(BoundNullCoalesceAssignExpr node)
    {
        var value = Visit(node.Value);
        if (ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Value = value });
    }

    protected override BoundExpr VisitMemberAssign(BoundMemberAssignExpr node)
    {
        var target = Visit(node.Target);
        var value = Visit(node.Value);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Target = target, Value = value });
    }

    protected override BoundExpr VisitMemberCompoundAssign(BoundMemberCompoundAssignExpr node)
    {
        var target = Visit(node.Target);
        var value = Visit(node.Value);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Target = target, Value = value });
    }

    protected override BoundExpr VisitMemberNullCoalesceAssign(BoundMemberNullCoalesceAssignExpr node)
    {
        var target = Visit(node.Target);
        var value = Visit(node.Value);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Target = target, Value = value });
    }

    protected override BoundExpr VisitMemberIncrement(BoundMemberIncrementExpr node)
    {
        var target = Visit(node.Target);
        if (ReferenceEquals(target, node.Target)) return node;
        return CopyMetadata(node, node with { Target = target });
    }

    protected override BoundExpr VisitIndexAssign(BoundIndexAssignExpr node)
    {
        var target = Visit(node.Target);
        var index = Visit(node.Index);
        var value = Visit(node.Value);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(index, node.Index) && ReferenceEquals(value, node.Value))
            return node;
        return CopyMetadata(node, node with { Target = target, Index = index, Value = value });
    }

    protected override BoundExpr VisitIndexCompoundAssign(BoundIndexCompoundAssignExpr node)
    {
        var target = Visit(node.Target);
        var index = Visit(node.Index);
        var value = Visit(node.Value);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(index, node.Index) && ReferenceEquals(value, node.Value))
            return node;
        return CopyMetadata(node, node with { Target = target, Index = index, Value = value });
    }

    protected override BoundExpr VisitIndexNullCoalesceAssign(BoundIndexNullCoalesceAssignExpr node)
    {
        var target = Visit(node.Target);
        var index = Visit(node.Index);
        var value = Visit(node.Value);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(index, node.Index) && ReferenceEquals(value, node.Value))
            return node;
        return CopyMetadata(node, node with { Target = target, Index = index, Value = value });
    }

    protected override BoundExpr VisitIndexIncrement(BoundIndexIncrementExpr node)
    {
        var target = Visit(node.Target);
        var index = Visit(node.Index);
        if (ReferenceEquals(target, node.Target) && ReferenceEquals(index, node.Index)) return node;
        return CopyMetadata(node, node with { Target = target, Index = index });
    }

    protected override BoundExpr VisitMultiDimIndexAssign(BoundMultiDimIndexAssignExpr node)
    {
        var target = Visit(node.Target);
        var indices = RewriteImmutableArray(node.Indices, out var indicesChanged);
        var value = Visit(node.Value);
        if (ReferenceEquals(target, node.Target) && !indicesChanged && ReferenceEquals(value, node.Value))
            return node;
        return CopyMetadata(node, node with { Target = target, Indices = indices, Value = value });
    }

    protected override BoundExpr VisitDeconstruction(BoundDeconstructionExpr node)
    {
        var value = Visit(node.ValueExpression);
        if (ReferenceEquals(value, node.ValueExpression)) return node;
        return CopyMetadata(node, node with { ValueExpression = value });
    }

    protected override BoundExpr VisitObjectCreation(BoundObjectCreationExpr node)
    {
        var args = RewriteImmutableArray(node.Arguments, out var argsChanged);
        var entries = RewriteInitializerEntries(node.InitializerEntries, out var entriesChanged);
        if (!argsChanged && !entriesChanged) return node;
        return CopyMetadata(node, node with { Arguments = args, InitializerEntries = entries });
    }

    protected override BoundExpr VisitObjectLiteral(BoundObjectLiteralExpr node)
    {
        var changed = false;
        var props = ImmutableArray.CreateBuilder<BoundObjectLiteralProperty>(node.Properties.Length);
        foreach (var p in node.Properties)
        {
            var newValue = Visit(p.Value);
            if (!ReferenceEquals(newValue, p.Value))
            {
                props.Add(new BoundObjectLiteralProperty(p.PropertyName, newValue, p.IsSpread));
                changed = true;
            }
            else
            {
                props.Add(p);
            }
        }
        if (!changed) return node;
        return CopyMetadata(node, node with { Properties = props.MoveToImmutable() });
    }

    protected override BoundExpr VisitCollectionCreation(BoundCollectionCreationExpr node)
    {
        var elements = RewriteImmutableArray(node.Elements, out var changed);
        if (!changed) return node;
        return CopyMetadata(node, node with { Elements = elements });
    }

    protected override BoundExpr VisitArrayAllocation(BoundArrayAllocationExpr node)
    {
        var sizes = RewriteImmutableArray(node.Sizes, out var changed);
        if (!changed) return node;
        return CopyMetadata(node, node with { Sizes = sizes });
    }


    protected override BoundExpr VisitMultiDimArrayInit(BoundMultiDimArrayInitExpr node)
    {
        var sizesChanged = false;
        ImmutableArray<BoundExpr>? sizes = node.ExplicitSizes;
        if (node.ExplicitSizes != null)
        {
            sizes = RewriteImmutableArray(node.ExplicitSizes.Value, out sizesChanged);
        }
        var flatValues = RewriteImmutableArray(node.FlatValues, out var valuesChanged);
        if (!sizesChanged && !valuesChanged) return node;
        return CopyMetadata(node, node with { ExplicitSizes = sizes, FlatValues = flatValues });
    }


    protected override BoundExpr VisitTuple(BoundTupleExpr node)
    {
        var elements = RewriteImmutableArray(node.Elements, out var changed);
        if (!changed) return node;
        return CopyMetadata(node, node with { Elements = elements });
    }

    protected override BoundExpr VisitSpread(BoundSpreadExpr node)
    {
        var expr = Visit(node.Expression);
        if (ReferenceEquals(expr, node.Expression)) return node;
        return CopyMetadata(node, node with { Expression = expr });
    }

    protected override BoundExpr VisitVariableDecl(BoundVariableDeclExpr node)
    {
        var init = Visit(node.Initializer);
        if (ReferenceEquals(init, node.Initializer)) return node;
        return CopyMetadata(node, node with { Initializer = init });
    }

    protected override BoundExpr VisitBlock(BoundBlockExpr node)
    {
        var stmts = RewriteImmutableArray(node.Statements, out var stmtsChanged);
        var ret = node.ReturnExpr != null ? Visit(node.ReturnExpr) : null;
        if (!stmtsChanged && ReferenceEquals(ret, node.ReturnExpr)) return node;
        return CopyMetadata(node, node with { Statements = stmts, ReturnExpr = ret });
    }

    protected override BoundExpr VisitReturn(BoundReturnExpr node)
    {
        var value = node.Value != null ? Visit(node.Value) : null;
        if (ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Value = value });
    }

    protected override BoundExpr VisitBreak(BoundBreakExpr node) => node;

    protected override BoundExpr VisitContinue(BoundContinueExpr node) => node;

    protected override BoundExpr VisitIfStatement(BoundIfStatementExpr node)
    {
        var cond = Visit(node.Condition);
        var then = RewriteImmutableArray(node.ThenStatements, out var thenChanged);
        var @else = RewriteImmutableArray(node.ElseStatements, out var elseChanged);
        if (ReferenceEquals(cond, node.Condition) && !thenChanged && !elseChanged) return node;
        return CopyMetadata(node, node with { Condition = cond, ThenStatements = then, ElseStatements = @else });
    }

    protected override BoundExpr VisitWhile(BoundWhileExpr node)
    {
        var cond = Visit(node.Condition);
        var body = RewriteImmutableArray(node.Body, out var changed);
        if (ReferenceEquals(cond, node.Condition) && !changed) return node;
        return CopyMetadata(node, node with { Condition = cond, Body = body });
    }

    protected override BoundExpr VisitDoWhile(BoundDoWhileExpr node)
    {
        var body = RewriteImmutableArray(node.Body, out var changed);
        var cond = Visit(node.Condition);
        if (!changed && ReferenceEquals(cond, node.Condition)) return node;
        return CopyMetadata(node, node with { Body = body, Condition = cond });
    }

    protected override BoundExpr VisitFor(BoundForExpr node)
    {
        var inits = RewriteImmutableArray(node.Initializers, out var initsChanged);
        var cond = node.Condition != null ? Visit(node.Condition) : null;
        var incs = RewriteImmutableArray(node.Increments, out var incsChanged);
        var body = RewriteImmutableArray(node.Body, out var bodyChanged);
        if (!initsChanged && ReferenceEquals(cond, node.Condition) && !incsChanged && !bodyChanged)
            return node;
        return CopyMetadata(node, node with { Initializers = inits, Condition = cond, Increments = incs, Body = body });
    }

    protected override BoundExpr VisitForEach(BoundForEachExpr node)
    {
        var collection = Visit(node.Collection);
        var body = RewriteImmutableArray(node.Body, out var changed);
        if (ReferenceEquals(collection, node.Collection) && !changed) return node;
        return CopyMetadata(node, node with { Collection = collection, Body = body });
    }

    protected override BoundExpr VisitSwitchStatement(BoundSwitchStatementExpr node)
    {
        var expr = Visit(node.Expression);
        var casesChanged = false;
        var cases = ImmutableArray.CreateBuilder<BoundSwitchCase>(node.Cases.Length);
        foreach (var c in node.Cases)
        {
            var guard = c.WhenGuard != null ? Visit(c.WhenGuard) : null;
            var stmts = RewriteImmutableArray(c.Statements, out var stmtsChanged);
            if (!ReferenceEquals(guard, c.WhenGuard) || stmtsChanged)
            {
                cases.Add(new BoundSwitchCase(c.CasePattern, guard, stmts));
                casesChanged = true;
            }
            else
            {
                cases.Add(c);
            }
        }
        if (ReferenceEquals(expr, node.Expression) && !casesChanged) return node;
        return CopyMetadata(node, node with { Expression = expr, Cases = cases.MoveToImmutable() });
    }

    protected override BoundExpr VisitSwitchExpression(BoundSwitchExpressionExpr node)
    {
        var expr = Visit(node.Expression);
        var armsChanged = false;
        var arms = ImmutableArray.CreateBuilder<BoundSwitchExpressionArm>(node.Arms.Length);
        foreach (var a in node.Arms)
        {
            var guard = a.WhenGuard != null ? Visit(a.WhenGuard) : null;
            var value = Visit(a.Value);
            if (!ReferenceEquals(guard, a.WhenGuard) || !ReferenceEquals(value, a.Value))
            {
                arms.Add(new BoundSwitchExpressionArm(a.Pattern, guard, value));
                armsChanged = true;
            }
            else
            {
                arms.Add(a);
            }
        }
        if (ReferenceEquals(expr, node.Expression) && !armsChanged) return node;
        return CopyMetadata(node, node with { Expression = expr, Arms = arms.MoveToImmutable() });
    }

    protected override BoundExpr VisitUsingStatement(BoundUsingStatementExpr node)
    {
        var resource = Visit(node.Resource);
        var body = Visit(node.Body);
        if (ReferenceEquals(resource, node.Resource) && ReferenceEquals(body, node.Body)) return node;
        return CopyMetadata(node, node with { Resource = resource, Body = body });
    }

    protected override BoundExpr VisitLockStatement(BoundLockStatementExpr node)
    {
        var lockObj = Visit(node.LockObject);
        var body = Visit(node.Body);
        if (ReferenceEquals(lockObj, node.LockObject) && ReferenceEquals(body, node.Body)) return node;
        return CopyMetadata(node, node with { LockObject = lockObj, Body = body });
    }

    protected override BoundExpr VisitGoto(BoundGotoExpr node) => node;

    protected override BoundExpr VisitGotoCase(BoundGotoCaseExpr node)
    {
        var value = Visit(node.Value);
        if (ReferenceEquals(value, node.Value)) return node;
        return CopyMetadata(node, node with { Value = value });
    }

    protected override BoundExpr VisitGotoDefault(BoundGotoDefaultExpr node) => node;

    protected override BoundExpr VisitThrow(BoundThrowExpr node)
    {
        if (node.Expression == null) return node;
        var expr = Visit(node.Expression);
        if (ReferenceEquals(expr, node.Expression)) return node;
        return CopyMetadata(node, node with { Expression = expr });
    }


    protected override BoundExpr VisitTryCatchFinally(BoundTryCatchFinallyExpr node)
    {
        var tryBody = RewriteImmutableArray(node.TryBody, out var tryChanged);
        var catchesChanged = false;
        var catches = ImmutableArray.CreateBuilder<BoundCatchClause>(node.CatchClauses.Length);
        foreach (var c in node.CatchClauses)
        {
            var guard = c.WhenGuard != null ? Visit(c.WhenGuard) : null;
            var body = RewriteImmutableArray(c.Body, out var bodyChanged);
            if (!ReferenceEquals(guard, c.WhenGuard) || bodyChanged)
            {
                catches.Add(new BoundCatchClause(c.ExceptionTypeName, c.VariableName, guard, body, c.LocalId));
                catchesChanged = true;
            }
            else
            {
                catches.Add(c);
            }
        }
        var finallyBody = RewriteImmutableArray(node.FinallyBody, out var finallyChanged);
        if (!tryChanged && !catchesChanged && !finallyChanged) return node;
        return CopyMetadata(node, node with { TryBody = tryBody, CatchClauses = catches.MoveToImmutable(), FinallyBody = finallyBody });
    }

    private static BoundMemberAccessBase RewriteMemberAccessTarget(BoundMemberAccessBase ma, BoundExpr newTarget) => ma switch
    {
        BoundPropertyAccessExpr prop => prop with { Target = newTarget },
        BoundFieldAccessExpr field => field with { Target = newTarget },
        BoundMethodGroupExpr mg => mg with { Target = newTarget },
        BoundDynamicMemberAccessExpr dyn => dyn with { Target = newTarget },
        _ => throw new InvalidOperationException($"Unexpected member access type '{ma.GetType().Name}'")
    };

    private ImmutableArray<BoundExpr> RewriteImmutableArray(ImmutableArray<BoundExpr> items, out bool changed)
    {
        changed = false;
        var builder = ImmutableArray.CreateBuilder<BoundExpr>(items.Length);
        foreach (var item in items)
        {
            var rewritten = Visit(item);
            if (!ReferenceEquals(rewritten, item)) changed = true;
            builder.Add(rewritten);
        }
        return changed ? builder.MoveToImmutable() : items;
    }

    private ImmutableArray<BoundInitializerEntry> RewriteInitializerEntries(
        ImmutableArray<BoundInitializerEntry> entries, out bool changed)
    {
        changed = false;
        var builder = ImmutableArray.CreateBuilder<BoundInitializerEntry>(entries.Length);
        foreach (var e in entries)
        {
            var newValue = Visit(e.Value);
            var newKey = e.IndexerKey != null ? Visit(e.IndexerKey) : null;
            if (!ReferenceEquals(newValue, e.Value) || !ReferenceEquals(newKey, e.IndexerKey))
            {
                builder.Add(new BoundInitializerEntry(e.PropertyName, newValue, newKey));
                changed = true;
            }
            else
            {
                builder.Add(e);
            }
        }
        return changed ? builder.MoveToImmutable() : entries;
    }
}

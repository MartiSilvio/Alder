using CsEval.Parsing;

namespace CsEval.Compiled.Compilation.CompilerUnits;

/// <summary>
/// Static checker that determines whether an AST can be IL-compiled.
/// Returns null if compilable, or a failure reason string if not.
/// </summary>
internal static class CompileGuard
{
    /// <summary>
    /// Check if an AST can be IL-compiled.
    /// Uses iterative approach with explicit stack to avoid StackOverflowException on deep expressions.
    /// Returns null if compilable, or a failure reason string if not.
    /// </summary>
    internal static string? CanCompile(Expr expr)
    {
        var stack = new Stack<Expr>();
        stack.Push(expr);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            switch (current)
            {
                case LiteralExpr:
                case IdentifierExpr:
                case TypeReferenceExpr:
                case IncrementDecrementExpr:
                case BreakExpr:
                case ContinueExpr:
                case DefaultExpr:
                case NameofExpr:
                case TypeofExpr:
                case SizeofExpr:
                    // These are always compilable, no children to check
                    break;

                case MemberIncrementExpr mi:
                    stack.Push(mi.Object);
                    break;

                case IndexIncrementExpr ii:
                    stack.Push(ii.Object);
                    stack.Push(ii.Index);
                    break;

                case MemberNullCoalesceAssignExpr mnca:
                    stack.Push(mnca.Object);
                    stack.Push(mnca.Value);
                    break;

                case IndexNullCoalesceAssignExpr inca:
                    stack.Push(inca.Object);
                    stack.Push(inca.Index);
                    stack.Push(inca.Value);
                    break;

                case ObjectCreationExpr oc:
                    foreach (var arg in oc.Arguments)
                        stack.Push(arg);
                    if (oc.Initializer != null)
                        foreach (var entry in oc.Initializer.Entries)
                            stack.Push(entry.Value);
                    break;

                case MultiDimIndexAccessExpr mdia:
                    stack.Push(mdia.Object);
                    foreach (var idx in mdia.Indices) stack.Push(idx);
                    break;

                case MultiDimTypedArrayCreationExpr mdtac:
                    foreach (var size in mdtac.Sizes) stack.Push(size);
                    break;

                case MultiDimIndexAssignExpr mdiassign:
                    stack.Push(mdiassign.Object);
                    foreach (var idx in mdiassign.Indices) stack.Push(idx);
                    stack.Push(mdiassign.Value);
                    break;

                case TypedArrayCreationExpr tac:
                    stack.Push(tac.Size);
                    break;

                case TypedArrayLiteralExpr tal:
                    stack.Push(tal.Elements);
                    break;

                case TupleExpr tuple:
                    foreach (var element in tuple.Elements)
                        stack.Push(element.Expression);
                    break;

                case DeconstructionExpr deconstruction:
                    stack.Push(deconstruction.ValueExpression);
                    break;

                case ThrowExpr throwExpr:
                    stack.Push(throwExpr.Expression);
                    break;

                case TryCatchFinallyExpr tcf:
                    foreach (var stmt in tcf.TryBody)
                        stack.Push(stmt);
                    foreach (var clause in tcf.CatchClauses)
                    {
                        foreach (var stmt in clause.Body)
                            stack.Push(stmt);
                        if (clause.WhenGuard != null)
                            stack.Push(clause.WhenGuard);
                    }
                    if (tcf.FinallyBody != null)
                        foreach (var stmt in tcf.FinallyBody)
                            stack.Push(stmt);
                    break;

                case UsingStatementExpr usingStmt:
                    stack.Push(usingStmt.ResourceDeclaration);
                    stack.Push(usingStmt.Body);
                    break;

                case LockStatementExpr lockStmt:
                    stack.Push(lockStmt.LockObject);
                    stack.Push(lockStmt.Body);
                    break;

                case ThrowStatementExpr:
                    break;

                case UnaryExpr { Op.Type: TokenType.Minus or TokenType.Plus or TokenType.Bang or TokenType.Tilde } u:
                    stack.Push(u.Right);
                    break;

                case UnaryExpr u:
                    return $"Unsupported unary operator '{u.Op.Lexeme}'";

                case CastExpr cast:
                    stack.Push(cast.Expression);
                    break;

                case IsPatternExpr isExpr:
                {
                    stack.Push(isExpr.Expression);
                    var patternReason = CanCompilePattern(isExpr.Pattern);
                    if (patternReason != null)
                        return patternReason;
                    break;
                }

                case SwitchExpressionExpr se:
                    stack.Push(se.Expression);
                    foreach (var arm in se.Arms)
                    {
                        var patternReason = CanCompilePattern(arm.Pattern);
                        if (patternReason != null)
                            return patternReason;
                        if (arm.WhenGuard != null)
                            stack.Push(arm.WhenGuard);
                        stack.Push(arm.Value);
                    }
                    break;

                case AsExpr asExpr:
                    stack.Push(asExpr.Expression);
                    break;

                case BinaryExpr b when IsCompilableBinaryOp(b.Op.Type):
                    stack.Push(b.Left);
                    stack.Push(b.Right);
                    break;

                case BinaryExpr b:
                    return $"Unsupported binary operator '{b.Op.Lexeme}'";

                case LogicalExpr l:
                    stack.Push(l.Left);
                    stack.Push(l.Right);
                    break;

                case ConditionalExpr c:
                    stack.Push(c.Condition);
                    stack.Push(c.ThenBranch);
                    stack.Push(c.ElseBranch);
                    break;

                case NullCoalesceExpr n:
                    stack.Push(n.Left);
                    stack.Push(n.Right);
                    break;

                case MemberAccessExpr m:
                    stack.Push(m.Object);
                    break;

                case IndexAccessExpr idx:
                    stack.Push(idx.Object);
                    stack.Push(idx.Index);
                    break;

                case SliceExpr slice:
                    stack.Push(slice.Target);
                    if (slice.Start != null) stack.Push(slice.Start);
                    if (slice.End != null) stack.Push(slice.End);
                    break;

                case VariableDeclExpr v:
                    stack.Push(v.Initializer);
                    break;

                case AssignExpr a:
                    stack.Push(a.Value);
                    break;

                case CompoundAssignExpr ca when IsCompilableCompoundOp(ca.Op.Type):
                    stack.Push(ca.Value);
                    break;

                case CompoundAssignExpr ca:
                    return $"Unsupported compound operator '{ca.Op.Lexeme}'";

                case MemberCompoundAssignExpr mca when IsCompilableCompoundOp(mca.Operator):
                    stack.Push(mca.Object);
                    stack.Push(mca.Value);
                    break;

                case MemberCompoundAssignExpr:
                    return "Unsupported compound operator on member access";

                case IndexCompoundAssignExpr ica when IsCompilableCompoundOp(ica.Operator):
                    stack.Push(ica.Object);
                    stack.Push(ica.Index);
                    stack.Push(ica.Value);
                    break;

                case IndexCompoundAssignExpr:
                    return "Unsupported compound operator on index access";

                case IndexAssignExpr ia:
                    stack.Push(ia.Object);
                    stack.Push(ia.Index);
                    stack.Push(ia.Value);
                    break;

                case BlockExpr b:
                    foreach (var stmt in b.Statements)
                        stack.Push(stmt);
                    if (b.ReturnExpr != null)
                        stack.Push(b.ReturnExpr);
                    break;

                case IfStatementExpr i:
                    stack.Push(i.Condition);
                    foreach (var stmt in i.ThenStatements)
                        stack.Push(stmt);
                    if (i.ElseStatements != null)
                        foreach (var stmt in i.ElseStatements)
                            stack.Push(stmt);
                    break;

                case SwitchStatementExpr s:
                    stack.Push(s.Expression);
                    foreach (var c in s.Cases)
                    {
                        if (c.CasePattern != null)
                        {
                            var patternReason = CanCompilePattern(c.CasePattern);
                            if (patternReason != null)
                                return patternReason;
                        }
                        if (c.WhenGuard != null)
                            stack.Push(c.WhenGuard);
                        foreach (var stmt in c.Statements)
                            stack.Push(stmt);
                    }
                    break;

                case WhileStatementExpr w:
                    stack.Push(w.Condition);
                    foreach (var stmt in w.Body)
                        stack.Push(stmt);
                    break;

                case ForStatementExpr f:
                    foreach (var init in f.Initializers) stack.Push(init);
                    if (f.Condition != null) stack.Push(f.Condition);
                    foreach (var inc in f.Increments) stack.Push(inc);
                    foreach (var stmt in f.Body)
                        stack.Push(stmt);
                    break;

                case DoWhileStatementExpr d:
                    stack.Push(d.Condition);
                    foreach (var stmt in d.Body)
                        stack.Push(stmt);
                    break;

                case ForEachStatementExpr fe:
                    stack.Push(fe.Collection);
                    foreach (var stmt in fe.Body)
                        stack.Push(stmt);
                    break;

                case ReturnExpr r:
                    if (r.Value != null)
                        stack.Push(r.Value);
                    break;

                case CallExpr call:
                    stack.Push(call.Callee);
                    foreach (var arg in call.Arguments)
                    {
                        if (arg is NamedArgumentExpr namedArg)
                            stack.Push(namedArg.Value);
                        else if (arg is OutArgExpr)
                            { } // OutArgExpr is a leaf node, handled at compile time
                        else
                            stack.Push(arg);
                    }
                    break;

                case LambdaExpr lambda:
                    stack.Push(lambda.Body);
                    break;

                case ArrayLiteralExpr arr:
                    foreach (var elem in arr.Elements)
                        stack.Push(elem);
                    break;

                case ObjectLiteralExpr obj:
                    foreach (var (_, value) in obj.Properties)
                        stack.Push(value);
                    break;

                case NewExpr newExpr:
                    stack.Push(newExpr.Initializer);
                    break;

                case InterpolatedStringExpr interp:
                    foreach (var part in interp.Parts)
                        if (part is ExpressionPart ep)
                            stack.Push(ep.Expression);
                    break;

                case MemberAssignExpr ma:
                    stack.Push(ma.Object);
                    stack.Push(ma.Value);
                    break;

                case NullCoalesceAssignExpr nca:
                    stack.Push(nca.Value);
                    break;

                case SpreadExpr spread:
                    stack.Push(spread.Expression);
                    break;

                case NamedArgumentExpr namedArg:
                    stack.Push(namedArg.Value);
                    break;

                case OutArgExpr:
                    // OutArgExpr as standalone is invalid; inside CallExpr it's handled above
                    break;

                case CheckedExpr checkedExpr:
                    stack.Push(checkedExpr.Expression);
                    break;

                case RangeExpr range:
                    stack.Push(range.Start);
                    stack.Push(range.End);
                    break;

                case PipelineExpr pipeline:
                    stack.Push(pipeline.Left);
                    stack.Push(pipeline.Right);
                    break;

                case ChainedComparisonExpr chain:
                    foreach (var operand in chain.Operands)
                        stack.Push(operand);
                    break;

                default:
                    return $"Unsupported expression type '{current.GetType().Name}'";
            }
        }

        return null; // All expressions are compilable
    }

    /// <summary>
    /// Check if a pattern can be IL-compiled.
    /// Returns null if compilable, or a failure reason string if not.
    /// </summary>
    private static string? CanCompilePattern(Pattern pattern)
    {
        switch (pattern)
        {
            case ConstantPattern:
            case TypePattern:
            case VarPattern:
            case DiscardPattern:
                return null; // always compilable

            case NotPattern np:
                return CanCompilePattern(np.Operand);

            case AndPattern ap:
                return CanCompilePattern(ap.Left) ?? CanCompilePattern(ap.Right);

            case OrPattern op:
                return CanCompilePattern(op.Left) ?? CanCompilePattern(op.Right);

            case ParenthesizedPattern pp:
                return CanCompilePattern(pp.Inner);

            case RelationalPattern:
                return null; // compilable

            case PropertyPattern pp:
                foreach (var (_, subPattern) in pp.Properties)
                {
                    var subResult = CanCompilePattern(subPattern);
                    if (subResult != null)
                        return subResult;
                }
                return null;

            default:
                return $"Unsupported pattern type '{pattern.GetType().Name}'";
        }
    }

    private static bool IsCompilableCompoundOp(TokenType op) => op is
        TokenType.PlusEqual or TokenType.MinusEqual or TokenType.StarEqual or
        TokenType.SlashEqual or TokenType.PercentEqual or
        TokenType.AmpEqual or TokenType.PipeEqual or TokenType.CaretEqual or
        TokenType.LessLessEqual or TokenType.GreaterGreaterEqual or
        TokenType.GreaterGreaterGreaterEqual or TokenType.StarStarEqual;

    internal static bool IsCompilableBinaryOp(TokenType op)
    {
        if (op is TokenType.Plus or TokenType.Minus or TokenType.Star or
            TokenType.Slash or TokenType.Percent or
            TokenType.EqualEqual or TokenType.EqualEqualEqual or
            TokenType.BangEqual or TokenType.BangEqualEqual or
            TokenType.Less or TokenType.LessEqual or
            TokenType.Greater or TokenType.GreaterEqual or
            TokenType.Amp or TokenType.Pipe or TokenType.Caret or
            TokenType.LessLess or TokenType.GreaterGreater or
            TokenType.GreaterGreaterGreater or TokenType.StarStar or
            TokenType.In or TokenType.NotIn or
            TokenType.Like or TokenType.NotLike or
            TokenType.EqualTilde or TokenType.BangTilde or
            TokenType.LessEqualGreater)
            return true;

        return false;
    }
}

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using CsEval.Parsing;
// ReSharper disable UseCollectionExpression

namespace CsEval.Evaluation;

/// <summary>
/// Compiles CsEval AST nodes to System.Linq.Expressions delegates.
/// Thread-safe with global caching.
/// </summary>
internal static class ExpressionCompiler
{
    // Global cache for string-based expression compilation
    private static readonly ConcurrentDictionary<string, CompiledExpressionInfo> Cache = new();

    // Parameters for the compiled delegate
    private static readonly ParameterExpression ContextParam =
        Expression.Parameter(typeof(EvalContext), "context");
    private static readonly ParameterExpression OptionsParam =
        Expression.Parameter(typeof(CsEvalOptions), "options");
    private static readonly ParameterExpression CancellationParam =
        Expression.Parameter(typeof(CancellationToken), "cancellationToken");

    // Cached MethodInfo for helper methods
    private static readonly MethodInfo GetMethod =
        typeof(EvalContext).GetMethod("Get", new[] { typeof(string) })!;
    private static readonly MethodInfo IsTruthyMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.IsTruthy))!;
    private static readonly MethodInfo NegateMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Negate))!;
    private static readonly MethodInfo AddMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Add))!;
    private static readonly MethodInfo SubtractMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Subtract))!;
    private static readonly MethodInfo MultiplyMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Multiply))!;
    private static readonly MethodInfo DivideMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Divide))!;
    private static readonly MethodInfo ModuloMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.Modulo))!;
    private static readonly MethodInfo EqualsMethod =
        typeof(CompilerHelpers).GetMethod("Equals", new[] { typeof(object), typeof(object), typeof(CsEvalOptions) })!;
    private static readonly MethodInfo NotEqualsMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.NotEquals))!;
    private static readonly MethodInfo LessThanMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.LessThan))!;
    private static readonly MethodInfo LessThanOrEqualMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.LessThanOrEqual))!;
    private static readonly MethodInfo GreaterThanMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.GreaterThan))!;
    private static readonly MethodInfo GreaterThanOrEqualMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.GreaterThanOrEqual))!;
    private static readonly MethodInfo GetMemberMethod =
        typeof(CompilerHelpers).GetMethod(nameof(CompilerHelpers.GetMember))!;

    /// <summary>
    /// Get or create compiled delegate for an expression string.
    /// </summary>
    public static CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast)
    {
        return Cache.GetOrAdd(expressionText, _ => TryCompile(ast));
    }

    /// <summary>
    /// Attempt to compile an AST to a delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(Expr ast)
    {
        try
        {
            if (!CanCompile(ast, out var reason))
            {
                return new CompiledExpressionInfo(null, false, reason);
            }

            var body = CompileExpr(ast);
            var boxedBody = EnsureObjectReturn(body);

            var lambda = Expression.Lambda<CompiledExpression>(
                boxedBody,
                ContextParam,
                OptionsParam,
                CancellationParam
            );

            var compiled = lambda.Compile();
            return new CompiledExpressionInfo(compiled, true, null);
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message);
        }
    }

    /// <summary>
    /// Check if an AST node can be compiled.
    /// </summary>
    private static bool CanCompile(Expr expr, out string? reason)
    {
        reason = null;

        return expr switch
        {
            LiteralExpr => true,
            IdentifierExpr => true,
            GroupingExpr g => CanCompile(g.Expression, out reason),
            UnaryExpr u when u.Op.Type is TokenType.Minus or TokenType.Bang
                => CanCompile(u.Right, out reason),
            BinaryExpr b when IsCompilableBinaryOp(b.Op.Type)
                => CanCompileChildrenAndCheckObjectMerge(b, out reason),
            LogicalExpr l => CanCompile(l.Left, out reason) && CanCompile(l.Right, out reason),
            ConditionalExpr c => CanCompile(c.Condition, out reason)
                && CanCompile(c.ThenBranch, out reason)
                && CanCompile(c.ElseBranch, out reason),
            NullCoalesceExpr n => CanCompile(n.Left, out reason) && CanCompile(n.Right, out reason),
            MemberAccessExpr m => CanCompile(m.Object, out reason),
            _ => SetReason(out reason, $"Expression type '{expr.GetType().Name}' not compilable")
        };
    }

    private static bool CanCompileChildrenAndCheckObjectMerge(BinaryExpr b, out string? reason)
    {
        // For + operator, we need to check if both sides are literals or simple expressions
        // that won't involve object merging at runtime
        // For now, we only compile + if we can verify no object merging will occur
        // This means both operands must be numeric literals, string literals, or identifiers
        if (b.Op.Type == TokenType.Plus)
        {
            if (!IsSimpleAdditionOperand(b.Left) || !IsSimpleAdditionOperand(b.Right))
            {
                return SetReason(out reason, "Addition may involve object merging which requires tree-walking");
            }
        }

        return CanCompile(b.Left, out reason) && CanCompile(b.Right, out reason);
    }

    private static bool IsSimpleAdditionOperand(Expr expr)
    {
        return expr switch
        {
            LiteralExpr lit => lit.Value is null or string or int or long or double or float or decimal or bool,
            IdentifierExpr => true,
            GroupingExpr g => IsSimpleAdditionOperand(g.Expression),
            UnaryExpr u when u.Op.Type == TokenType.Minus => IsSimpleAdditionOperand(u.Right),
            BinaryExpr b => IsSimpleAdditionOperand(b.Left) && IsSimpleAdditionOperand(b.Right),
            MemberAccessExpr => true, // Property access result could be anything, but we allow it
            ConditionalExpr c => IsSimpleAdditionOperand(c.ThenBranch) && IsSimpleAdditionOperand(c.ElseBranch),
            NullCoalesceExpr n => IsSimpleAdditionOperand(n.Left) && IsSimpleAdditionOperand(n.Right),
            _ => false
        };
    }

    private static bool SetReason(out string? reason, string msg)
    {
        reason = msg;
        return false;
    }

    private static bool IsCompilableBinaryOp(TokenType op) => op is
        TokenType.Plus or TokenType.Minus or TokenType.Star or
        TokenType.Slash or TokenType.Percent or
        TokenType.EqualEqual or TokenType.BangEqual or
        TokenType.EqualEqualEqual or TokenType.BangEqualEqual or  // JavaScript
        TokenType.Less or TokenType.LessEqual or
        TokenType.Greater or TokenType.GreaterEqual;

    /// <summary>
    /// Compile an AST node to a System.Linq.Expression.
    /// </summary>
    private static Expression CompileExpr(Expr expr)
    {
        return expr switch
        {
            LiteralExpr lit => CompileLiteral(lit),
            IdentifierExpr id => CompileIdentifier(id),
            GroupingExpr g => CompileExpr(g.Expression),
            UnaryExpr u => CompileUnary(u),
            BinaryExpr b => CompileBinary(b),
            LogicalExpr l => CompileLogical(l),
            ConditionalExpr c => CompileConditional(c),
            NullCoalesceExpr n => CompileNullCoalesce(n),
            MemberAccessExpr m => CompileMemberAccess(m),
            _ => throw new NotSupportedException($"Cannot compile {expr.GetType().Name}")
        };
    }

    private static Expression CompileLiteral(LiteralExpr lit)
    {
        return Expression.Constant(lit.Value, typeof(object));
    }

    private static Expression CompileIdentifier(IdentifierExpr id)
    {
        // context.Get("varName")
        return Expression.Call(
            ContextParam,
            GetMethod,
            Expression.Constant(id.Name.Lexeme)
        );
    }

    private static Expression CompileUnary(UnaryExpr u)
    {
        var right = CompileExpr(u.Right);

        return u.Op.Type switch
        {
            TokenType.Minus => CompileNegation(right),
            TokenType.Bang => CompileNot(right),
            _ => throw new NotSupportedException($"Unary operator {u.Op.Type} not supported")
        };
    }

    private static Expression CompileNegation(Expression operand)
    {
        // CompilerHelpers.Negate(operand)
        return Expression.Call(NegateMethod, EnsureObjectReturn(operand));
    }

    private static Expression CompileNot(Expression operand)
    {
        // !CompilerHelpers.IsTruthy(operand)
        var isTruthy = Expression.Call(IsTruthyMethod, EnsureObjectReturn(operand));
        return Expression.Convert(Expression.Not(isTruthy), typeof(object));
    }

    private static Expression CompileBinary(BinaryExpr b)
    {
        var left = CompileExpr(b.Left);
        var right = CompileExpr(b.Right);

        // Get the helper method for this operator
        var method = b.Op.Type switch
        {
            TokenType.Plus => AddMethod,
            TokenType.Minus => SubtractMethod,
            TokenType.Star => MultiplyMethod,
            TokenType.Slash => DivideMethod,
            TokenType.Percent => ModuloMethod,
            TokenType.EqualEqual => EqualsMethod,
            TokenType.BangEqual => NotEqualsMethod,
            TokenType.EqualEqualEqual => EqualsMethod,     // JavaScript === (same as ==)
            TokenType.BangEqualEqual => NotEqualsMethod,   // JavaScript !== (same as !=)
            TokenType.Less => LessThanMethod,
            TokenType.LessEqual => LessThanOrEqualMethod,
            TokenType.Greater => GreaterThanMethod,
            TokenType.GreaterEqual => GreaterThanOrEqualMethod,
            _ => throw new NotSupportedException($"Binary operator {b.Op.Type} not supported")
        };

        return Expression.Call(
            method,
            EnsureObjectReturn(left),
            EnsureObjectReturn(right),
            OptionsParam
        );
    }

    private static Expression CompileLogical(LogicalExpr l)
    {
        var left = CompileExpr(l.Left);
        var right = CompileExpr(l.Right);

        var leftTruthy = Expression.Call(IsTruthyMethod, EnsureObjectReturn(left));
        var rightTruthy = Expression.Call(IsTruthyMethod, EnsureObjectReturn(right));

        // Short-circuit evaluation
        if (l.Op.Type == TokenType.PipePipe)
        {
            // left || right: if left is truthy, return true, else evaluate right
            return Expression.Condition(
                leftTruthy,
                Expression.Constant(true, typeof(object)),
                Expression.Convert(rightTruthy, typeof(object))
            );
        }
        else
        {
            // left && right: if left is falsy, return false, else evaluate right
            return Expression.Condition(
                leftTruthy,
                Expression.Convert(rightTruthy, typeof(object)),
                Expression.Constant(false, typeof(object))
            );
        }
    }

    private static Expression CompileConditional(ConditionalExpr c)
    {
        var condition = CompileExpr(c.Condition);
        var thenBranch = CompileExpr(c.ThenBranch);
        var elseBranch = CompileExpr(c.ElseBranch);

        var conditionTruthy = Expression.Call(IsTruthyMethod, EnsureObjectReturn(condition));

        return Expression.Condition(
            conditionTruthy,
            EnsureObjectReturn(thenBranch),
            EnsureObjectReturn(elseBranch)
        );
    }

    private static Expression CompileNullCoalesce(NullCoalesceExpr n)
    {
        var left = CompileExpr(n.Left);
        var right = CompileExpr(n.Right);

        // left ?? right
        return Expression.Coalesce(
            EnsureObjectReturn(left),
            EnsureObjectReturn(right)
        );
    }

    private static Expression CompileMemberAccess(MemberAccessExpr m)
    {
        var obj = CompileExpr(m.Object);

        // CompilerHelpers.GetMember(obj, "name", options, isNullSafe)
        return Expression.Call(
            GetMemberMethod,
            EnsureObjectReturn(obj),
            Expression.Constant(m.Name.Lexeme),
            OptionsParam,
            Expression.Constant(m.NullSafe)
        );
    }

    /// <summary>
    /// Ensure expression returns object? (box value types).
    /// </summary>
    private static Expression EnsureObjectReturn(Expression expr)
    {
        if (expr.Type == typeof(object))
            return expr;
        return Expression.Convert(expr, typeof(object));
    }

    /// <summary>
    /// Clear the compilation cache (for testing).
    /// </summary>
    public static void ClearCache() => Cache.Clear();
}

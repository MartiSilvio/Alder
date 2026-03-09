using CsEval.Compilation;
using CsEval.Binding;
using CsEval.Parsing;
using CsEval.Runtime;

namespace CsEval.Compiled.Compilation;

/// <summary>
/// Orchestrates AST-to-IL compilation via <see cref="CompilerContext"/>.
/// </summary>
internal static class ILExpressionCompiler
{
    /// <summary>
    /// Get or create compiled delegate for an expression string.
    /// </summary>
    public static CompiledExpressionInfo GetOrCompile(string expressionText, Expr ast, ExpressionCache cache, CsEvalOptions? options = null)
    {
        return cache.GetOrAdd(expressionText, _ => TryCompile(ast, options));
    }

    /// <summary>
    /// Attempt to compile an AST to a native IL delegate.
    /// </summary>
    public static CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options = null)
        => TryCompile(ast, options, null);

    public static CompiledExpressionInfo TryCompile(Expr ast, CsEvalOptions? options, CsEvalContext? typeHintContext)
    {
        try
        {
            var context = typeHintContext ?? new CsEvalContext(CsEvalConfig.Empty);
            var opts = options ?? CsEvalOptions.Default;

            var boundDelegate = TryCompileBound(ast, context, opts);
            if (boundDelegate != null)
            {
                return new CompiledExpressionInfo(CompiledBound, true, null, Pipeline: CompiledPipeline.Bound);

                object? CompiledBound(CsEvalContext ctx, CsEvalOptions optionsValue, CancellationToken ct)
                    => boundDelegate(ctx, optionsValue, ct);
            }

            var (ilDelegate, failureReason, failureException) = CompilerContext.TryCompile(ast, context, opts);

            if (ilDelegate != null)
            {
                return new CompiledExpressionInfo(Compiled, true, null, Pipeline: CompiledPipeline.Ast);

                object? Compiled(CsEvalContext ctx, CsEvalOptions optionsValue, CancellationToken ct)
                    => ilDelegate(ctx, optionsValue, ct);
            }

            return new CompiledExpressionInfo(null, false, failureReason, failureException);
        }
        catch (CsEvalDepthException)
        {
            throw; // Depth limits are recoverable — let them propagate so callers can surface them
        }
        catch (Exception ex)
        {
            return new CompiledExpressionInfo(null, false, ex.Message, ex);
        }
    }

    private static ILCompiledDelegate? TryCompileBound(Expr ast, CsEvalContext context, CsEvalOptions options)
    {
        if (!CanUseBoundCompiler(ast))
            return null;

        try
        {
            var binder = new CsEval.Binding.Binder();
            var bound = binder.Bind(ast, new BindingContext(context));

            var contextParam = LinqExpression.Parameter(typeof(CsEvalContext), "context");
            var optionsParam = LinqExpression.Parameter(typeof(CsEvalOptions), "options");
            var ctParam = LinqExpression.Parameter(typeof(CancellationToken), "ct");

            var emitter = new BoundExpressionEmitter(contextParam, optionsParam, ctParam);
            var body = emitter.Emit(bound);
            if (body.Type != typeof(object))
                body = LinqExpression.Convert(body, typeof(object));

            var lambda = LinqExpression.Lambda<ILCompiledDelegate>(body, contextParam, optionsParam, ctParam);
            return options.ExpressionCompiler.Compile(lambda);
        }
        catch (BindingNotSupportedException)
        {
            return null;
        }
    }

    private static bool CanUseBoundCompiler(Expr expr)
    {
        var pending = new Stack<Expr>();
        pending.Push(expr);

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            switch (current)
            {
                case LiteralExpr:
                case IdentifierExpr:
                case TypeReferenceExpr:
                    continue;

                case UnaryExpr unary:
                    pending.Push(unary.Right);
                    continue;

                case BinaryExpr binary:
                    if (IsArithmeticBinaryOperator(binary.Op.Type) &&
                        (binary.Left is LiteralExpr { IsConstant: true } ||
                         binary.Right is LiteralExpr { IsConstant: true }))
                    {
                        // Keep constant-expression numeric promotion on the mature AST compiler path for now.
                        return false;
                    }

                    pending.Push(binary.Right);
                    pending.Push(binary.Left);
                    continue;

                case CastExpr cast:
                    pending.Push(cast.Expression);
                    continue;

                case AsExpr asExpr:
                    pending.Push(asExpr.Expression);
                    continue;

                case NullCoalesceExpr coalesce:
                    pending.Push(coalesce.Right);
                    pending.Push(coalesce.Left);
                    continue;

                case ConditionalExpr conditional:
                    pending.Push(conditional.ElseBranch);
                    pending.Push(conditional.ThenBranch);
                    pending.Push(conditional.Condition);
                    continue;

                case MemberAccessExpr memberAccess:
                    pending.Push(memberAccess.Object);
                    continue;

                case IndexAccessExpr indexAccess:
                    pending.Push(indexAccess.Index);
                    pending.Push(indexAccess.Object);
                    continue;

                case CallExpr call:
                    if (call.TypeArguments is { Count: > 0 })
                        return false;

                    for (var i = call.Arguments.Count - 1; i >= 0; i--)
                    {
                        var argument = call.Arguments[i];
                        if (argument is NamedArgumentExpr or OutArgExpr)
                            return false;
                        pending.Push(argument);
                    }

                    pending.Push(call.Callee);
                    continue;

                default:
                    return false;
            }
        }

        return true;
    }

    private static bool IsArithmeticBinaryOperator(TokenType tokenType)
    {
        return tokenType is
            TokenType.Plus or
            TokenType.Minus or
            TokenType.Star or
            TokenType.Slash or
            TokenType.Percent;
    }
}

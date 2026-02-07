using System.Linq.Expressions;
using CsEval.Runtime;

namespace CsEval.Compilation;

/// <summary>
/// Scope management utilities for the IL compiler.
/// Handles push/pop of child contexts via TryFinally blocks.
/// </summary>
internal sealed class CompilerHelpers
{
    private readonly CompilerContext _ctx;

    internal CompilerHelpers(CompilerContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Wraps a block of code in a scope (TryFinally for cleanup).
    /// </summary>
    internal LinqExpression Scoped(Func<LinqExpression> bodyFactory)
    {
        // 1. Enter scope (assigns new child context)
        var enterExpr = EnterScopeExpr(out var parentVar);

        // 2. Compile body (uses current context)
        var body = bodyFactory();

        // 3. Exit scope (restores parent context)
        // cleanup is guaranteed by TryFinally
        var exitExpr = ExitScopeExpr(parentVar);

        return LinqExpression.Block(
            new[] { parentVar },
            enterExpr,
            LinqExpression.TryFinally(
                body,
                exitExpr));
    }

    /// <summary>
    /// Create expressions to enter a new scope (child context).
    /// Returns the expression that performs the scope entry.
    /// The parentVar output parameter receives the variable that stores the parent context.
    /// </summary>
    private LinqExpression EnterScopeExpr(out ParameterExpression parentVar)
    {
        parentVar = LinqExpression.Variable(typeof(CsEvalContext), $"parent{_ctx.ContextStack.Count}");
        _ctx.ContextStack.Push(parentVar);

        var currentContextVar = _ctx.CurrentContext as ParameterExpression;
        if (currentContextVar == null)
        {
            // First scope - current context is the parameter
            currentContextVar = _ctx.ContextParam;
        }

        // Save parent and create child
        var saveParent = LinqExpression.Assign(parentVar, _ctx.CurrentContext);
        var createChild = LinqExpression.Assign(
            _ctx.CurrentContext,
            LinqExpression.Call(_ctx.CurrentContext, CompilerContext.CreateChildMethod));

        return LinqExpression.Block(saveParent, createChild);
    }

    /// <summary>
    /// Create expression to exit current scope (restore parent context).
    /// </summary>
    private LinqExpression ExitScopeExpr(ParameterExpression parentVar)
    {
        _ctx.ContextStack.Pop();
        return LinqExpression.Assign(_ctx.CurrentContext, parentVar);
    }
}

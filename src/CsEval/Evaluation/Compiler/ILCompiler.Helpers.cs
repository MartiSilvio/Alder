using System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

namespace CsEval.Evaluation.Compiler;

internal sealed partial class ILCompiler
{
    #region Helpers

    private LinqExpression CompileCancellationCheck()
    {
        return LinqExpression.Call(_ctParam, ThrowIfCancellationRequestedMethod);
    }

    private LinqExpression CompileIterationCheck()
    {
        // _iterationCount++; CheckIterationLimit(_iterationCount, options);
        return LinqExpression.Block(
            LinqExpression.PostIncrementAssign(_iterationCount),
            LinqExpression.Call(CheckIterationLimitMethod, _iterationCount, _optionsParam));
    }

    /// <summary>
    /// Wraps a block of code in a scope (TryFinally for cleanup).
    /// </summary>
    private LinqExpression Scoped(Func<LinqExpression> bodyFactory)
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
        parentVar = LinqExpression.Variable(typeof(EvalContext), $"parent{_contextStack.Count}");
        _contextStack.Push(parentVar);

        var currentContextVar = _currentContext as ParameterExpression;
        if (currentContextVar == null)
        {
            // First scope - current context is the parameter
            currentContextVar = _contextParam;
        }

        // Save parent and create child
        var saveParent = LinqExpression.Assign(parentVar, _currentContext);
        var createChild = LinqExpression.Assign(
            _currentContext,
            LinqExpression.Call(_currentContext, CreateChildMethod));

        return LinqExpression.Block(saveParent, createChild);
    }

    /// <summary>
    /// Create expression to exit current scope (restore parent context).
    /// </summary>
    private LinqExpression ExitScopeExpr(ParameterExpression parentVar)
    {
        _contextStack.Pop();
        return LinqExpression.Assign(_currentContext, parentVar);
    }

    #endregion
}

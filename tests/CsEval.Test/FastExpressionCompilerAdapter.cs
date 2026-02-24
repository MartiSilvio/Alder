using FastExpressionCompiler;

namespace CsEval.Test;

/// <summary>
/// IExpressionCompiler adapter that delegates to FastExpressionCompiler.
/// </summary>
public sealed class FastExpressionCompilerAdapter : IExpressionCompiler
{
    public TDelegate Compile<TDelegate>(System.Linq.Expressions.Expression<TDelegate> expression)
        where TDelegate : Delegate
        => expression.CompileFast<TDelegate>();
}

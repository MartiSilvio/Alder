using System.Linq.Expressions;
using FastExpressionCompiler;

namespace CsEval.Test._Infrastructure;

public sealed class FastExpressionCompilerAdapter : IExpressionCompiler
{
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate
        => expression.CompileFast<TDelegate>(ifFastFailedReturnNull: true, CompilerFlags.ThrowOnNotSupportedExpression);
}

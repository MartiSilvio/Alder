using System.Linq.Expressions;
using FastExpressionCompiler;

namespace Alder.Benchmarks;

/// <summary>
/// No-fallback FEC adapter: throws if FEC cannot compile via its fast IL path.
/// ifFastFailedReturnNull: false prevents silent fallback to standard Expression.Compile().
/// </summary>
public sealed class FastExpressionCompilerAdapter : IExpressionCompiler
{
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate
        => expression.CompileFast<TDelegate>(ifFastFailedReturnNull: false, CompilerFlags.ThrowOnNotSupportedExpression);
}

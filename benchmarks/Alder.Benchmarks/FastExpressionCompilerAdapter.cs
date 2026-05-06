using System.Linq.Expressions;
using FastExpressionCompiler;

namespace Alder.Benchmarks;

/// <summary>
/// Forces FastExpressionCompiler to fail instead of silently falling back to <c>Expression.Compile()</c>.
/// Benchmark results should reflect the FEC backend only.
/// </summary>
public sealed class FastExpressionCompilerAdapter : IExpressionCompiler
{
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate
        => expression.CompileFast<TDelegate>(ifFastFailedReturnNull: false, CompilerFlags.ThrowOnNotSupportedExpression);
}

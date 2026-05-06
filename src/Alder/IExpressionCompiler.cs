using System.Linq.Expressions;

namespace Alder;

/// <summary>
/// Extension point for compiling LINQ expression trees to delegates.
/// </summary>
public interface IExpressionCompiler
{
    /// <summary>Compiles a LINQ expression tree into a delegate.</summary>
    /// <typeparam name="TDelegate">The delegate type to compile to.</typeparam>
    /// <param name="expression">The LINQ expression tree to compile.</param>
    /// <returns>The compiled delegate.</returns>
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}

/// <summary>
/// Default implementation that delegates to <see cref="LambdaExpression.Compile()"/>.
/// </summary>
public sealed class DefaultExpressionCompiler : IExpressionCompiler
{
    /// <summary>Singleton instance.</summary>
    public static readonly DefaultExpressionCompiler Instance = new();
    private DefaultExpressionCompiler() { }

    /// <inheritdoc/>
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate => expression.Compile();
}

using System.Linq.Expressions;

namespace Alder;

/// <summary>
/// Extension point for controlling how LINQ expression trees are compiled to delegates.
/// Implement this interface to substitute an alternative compiler backend.
/// </summary>
public interface IExpressionCompiler
{
    /// <summary>
    /// Compiles a LINQ expression tree into a delegate of the specified type.
    /// </summary>
    /// <typeparam name="TDelegate">The delegate type to compile to.</typeparam>
    /// <param name="expression">The LINQ expression tree to compile.</param>
    /// <returns>The compiled delegate.</returns>
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}

/// <summary>
/// Default implementation that delegates to <see cref="System.Linq.Expressions.LambdaExpression.Compile()"/>.
/// </summary>
public sealed class DefaultExpressionCompiler : IExpressionCompiler
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static readonly DefaultExpressionCompiler Instance = new();
    private DefaultExpressionCompiler() { }

    /// <inheritdoc/>
    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate => expression.Compile();
}

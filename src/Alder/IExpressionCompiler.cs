using System.Linq.Expressions;

namespace Alder;

/// <summary>
/// Extension point for controlling how LINQ expression trees are compiled to delegates.
/// Implement this interface to substitute an alternative compiler backend.
/// </summary>
public interface IExpressionCompiler
{
    TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate;
}

/// <summary>
/// Default implementation that delegates to <see cref="System.Linq.Expressions.LambdaExpression.Compile()"/>.
/// </summary>
public sealed class DefaultExpressionCompiler : IExpressionCompiler
{
    public static readonly DefaultExpressionCompiler Instance = new();
    private DefaultExpressionCompiler() { }

    public TDelegate Compile<TDelegate>(Expression<TDelegate> expression)
        where TDelegate : Delegate => expression.Compile();
}

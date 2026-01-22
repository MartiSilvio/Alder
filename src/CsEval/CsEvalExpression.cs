using CsEval.Parsing;

namespace CsEval;

/// <summary>
/// Represents a pre-parsed expression that can be evaluated multiple times
/// with different variable values without re-parsing.
/// </summary>
public sealed class CsEvalExpression
{
    internal Expr Ast { get; }

    /// <summary>
    /// The original expression string.
    /// </summary>
    public string Expression { get; }

    internal CsEvalExpression(string expression, Expr ast)
    {
        Expression = expression;
        Ast = ast;
    }
}

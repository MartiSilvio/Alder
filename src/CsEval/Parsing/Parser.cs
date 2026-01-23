namespace CsEval.Parsing;

/// <summary>
/// Recursive descent parser for CsEval expressions.
/// Split into partial classes:
/// - Parser.cs: Core utilities and entry point
/// - Parser.Expressions.cs: Expression precedence hierarchy
/// - Parser.Primary.cs: Primary expressions and literals
/// - Parser.Statements.cs: Statement and control flow parsing
/// </summary>
public sealed partial class Parser(List<Token> tokens)
{
    private int _current;

    public Expr Parse()
    {
        var expr = ParseExpression();
        if (!IsAtEnd())
            throw new ParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
        return expr;
    }

    #region Token Utilities

    private bool Match(params TokenType[] types)
    {
        foreach (var type in types)
        {
            if (Check(type))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private bool MatchCompoundAssignment(out Token op)
    {
        if (Match(TokenType.PlusEqual, TokenType.MinusEqual, TokenType.StarEqual,
                  TokenType.SlashEqual, TokenType.PercentEqual, TokenType.AmpEqual,
                  TokenType.PipeEqual, TokenType.CaretEqual, TokenType.LessLessEqual,
                  TokenType.GreaterGreaterEqual))
        {
            op = Previous();
            return true;
        }
        op = default;
        return false;
    }

    private bool MatchTypeKeyword(out Token typeToken)
    {
        // All C# primitive type keywords that can be used for variable declarations
        if (Check(TokenType.Int) || Check(TokenType.Long) || Check(TokenType.Double) ||
            Check(TokenType.Float) || Check(TokenType.Decimal) || Check(TokenType.StringType) ||
            Check(TokenType.Bool) || Check(TokenType.Object) ||
            Check(TokenType.Sbyte) || Check(TokenType.Byte) || Check(TokenType.Short) ||
            Check(TokenType.Ushort) || Check(TokenType.Uint) || Check(TokenType.Ulong) ||
            Check(TokenType.Char))
        {
            typeToken = Advance();
            return true;
        }
        typeToken = default;
        return false;
    }

    private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;

    private Token Advance()
    {
        if (!IsAtEnd()) _current++;
        return Previous();
    }

    private bool IsAtEnd() => Peek().Type == TokenType.Eof;

    private Token Peek() => tokens[_current];

    private Token Previous() => tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new ParserException($"{message} at {Peek().Line}:{Peek().Column}");
    }

    #endregion
}

public class ParserException(string message) : Exception(message);

namespace CsEval.Parsing;

/// <summary>
/// Recursive descent parser for CsEval expressions.
/// Split into partial classes:
/// - Parser.cs: Core utilities and entry point
/// - Parser.Expressions.cs: Expression precedence hierarchy
/// - Parser.Primary.cs: Primary expressions and literals
/// - Parser.Statements.cs: Statement and control flow parsing
/// </summary>
public sealed partial class Parser
{
    private readonly List<Token> _tokens;
    private int _current;

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }

    public Expr Parse()
    {
        if (IsStatementKeyword())
            return ParseProgram();

        var expr = ParseExpression();

        if (IsAtEnd())
            return expr;

        if (!Check(TokenType.Semicolon))
            throw new ParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");

        _current = 0;
        return ParseProgram();
    }

    private Expr ParseProgram()
    {
        var statements = new List<Expr>();

        while (!IsAtEnd())
        {
            if (IsStatementKeyword())
            {
                var stmt = ParseStatement();
                if (stmt != null)
                    statements.Add(stmt);
            }
            else
            {
                var expr = ParseExpression();

                if (Check(TokenType.Semicolon))
                {
                    Advance();
                    statements.Add(expr);
                }
                else if (IsAtEnd())
                {
                    return new BlockExpr(statements, expr);
                }
                else
                {
                    throw new ParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
                }
            }
        }

        if (statements.Count > 0)
            return new BlockExpr(statements, null);

        throw new ParserException("Empty expression");
    }

    private bool IsStatementKeyword()
    {
        return Check(TokenType.Return) || Check(TokenType.Break) || Check(TokenType.Continue) ||
               Check(TokenType.If) || Check(TokenType.While) || Check(TokenType.For) ||
               Check(TokenType.Do) || Check(TokenType.Foreach) || Check(TokenType.Switch) ||
               Check(TokenType.Var) ||
               Check(TokenType.Int) || Check(TokenType.Long) || Check(TokenType.Double) ||
               Check(TokenType.Float) || Check(TokenType.Decimal) || Check(TokenType.StringType) ||
               Check(TokenType.Bool) || Check(TokenType.Object) ||
               Check(TokenType.Sbyte) || Check(TokenType.Byte) || Check(TokenType.Short) ||
               Check(TokenType.Ushort) || Check(TokenType.Uint) || Check(TokenType.Ulong) ||
               Check(TokenType.Char);
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

            if (Check(TokenType.Question))
            {
                Advance();
                typeToken = typeToken with { Lexeme = typeToken.Lexeme + "?" };
            }

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

    private Token Peek() => _tokens[_current];

    private Token PeekNext() => _current + 1 < _tokens.Count ? _tokens[_current + 1] : _tokens[^1];

    private Token Previous() => _tokens[_current - 1];

    private Token Consume(TokenType type, string message)
    {
        if (Check(type)) return Advance();
        throw new ParserException($"{message} at {Peek().Line}:{Peek().Column}");
    }

    #endregion
}

public class ParserException(string message) : Exception(message);

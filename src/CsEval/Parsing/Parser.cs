namespace CsEval.Parsing;

/// <summary>
/// Recursive descent parser for CsEval expressions.
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
            throw new CsEvalParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");

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
                    throw new CsEvalParserException($"Unexpected token '{Peek().Lexeme}' at {Peek().Line}:{Peek().Column}");
                }
            }
        }

        if (statements.Count > 0)
            return new BlockExpr(statements, null);

        throw new CsEvalParserException("Empty expression");
    }

    private bool IsStatementKeyword()
    {
        // Control flow keywords are always statement keywords
        if (Check(TokenType.Return) || Check(TokenType.Break) || Check(TokenType.Continue) ||
            Check(TokenType.If) || Check(TokenType.While) || Check(TokenType.For) ||
            Check(TokenType.Do) || Check(TokenType.Foreach) || Check(TokenType.Switch) ||
            Check(TokenType.Try) || Check(TokenType.Var))
            return true;

        // Type keywords are statement keywords ONLY if NOT followed by '.' (for static member access like double.NaN)
        if (IsTypeKeyword(Peek().Type) && PeekNext().Type != TokenType.Dot)
            return true;

        return false;
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
        if (IsTypeKeyword(Peek().Type))
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

    private static bool IsTypeKeyword(TokenType type) =>
        type is TokenType.Int or TokenType.Long or TokenType.Double or
                TokenType.Float or TokenType.Decimal or TokenType.StringType or
                TokenType.Bool or TokenType.Object or TokenType.Sbyte or
                TokenType.Byte or TokenType.Short or TokenType.Ushort or
                TokenType.Uint or TokenType.Ulong or TokenType.Char;

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
        throw new CsEvalParserException($"{message} at {Peek().Line}:{Peek().Column}");
    }

    #endregion
}

public class CsEvalParserException(string message) : CsEvalException(message);

using Alder.Diagnostics;

namespace Alder.Parsing;

internal sealed partial class ExpressionParser
{
    /// <summary>
    /// Attempts to parse generic type arguments: &lt;T1, T2, ...&gt;
    /// Uses TryParseTypeName for each argument to support nested generics, dotted names,
    /// and nullable types: Method&lt;Dictionary&lt;string, int&gt;&gt;(), Method&lt;int?&gt;().
    /// Uses lookahead to disambiguate from less-than operator.
    /// </summary>
    private bool TryParseTypeArguments(out List<string> typeArgs)
    {
        typeArgs = [];

        var startPos = State.Current;
        if (!Check(TokenType.Less))
            return false;

        Advance(); // consume '<'

        while (true)
        {
            var typeName = TryParseTypeName();
            if (typeName == null)
            {
                State.Current = startPos;
                typeArgs.Clear();
                return false;
            }

            typeArgs.Add(NormalizeTypeNameString(typeName));

            if (Match(TokenType.Comma))
                continue;

            if (MatchClosingAngleBracket())
            {
                if (Check(TokenType.LeftParen) || Check(TokenType.Dot) || Check(TokenType.QuestionDot))
                    return true;

                State.Current = startPos;
                typeArgs.Clear();
                return false;
            }

            State.Current = startPos;
            typeArgs.Clear();
            return false;
        }
    }

    /// <summary>
    /// Normalizes a type name string from C# keyword form to CLR form.
    /// Handles both simple names (int -> Int32) and compound names (preserving generics/arrays).
    /// </summary>
    private static string NormalizeTypeNameString(string typeName)
    {
        return typeName switch
        {
            "int" => "Int32",
            "long" => "Int64",
            "short" => "Int16",
            "byte" => "Byte",
            "sbyte" => "SByte",
            "ushort" => "UInt16",
            "uint" => "UInt32",
            "ulong" => "UInt64",
            "float" => "Single",
            "double" => "Double",
            "decimal" => "Decimal",
            "bool" => "Boolean",
            "char" => "Char",
            "string" => "String",
            "object" => "Object",
            "dynamic" => "Object",
            "nint" => "IntPtr",
            "nuint" => "UIntPtr",
            _ => typeName
        };
    }

    private Expr FinishCall(Expr callee, List<string>? typeArgs, int mark)
    {
        var arguments = new List<Expr>();
        if (!Check(TokenType.RightParen))
        {
            do
            {
                arguments.Add(ParseArgument());
            } while (Match(TokenType.Comma));
        }

        Consume(TokenType.RightParen, "Expected ')' after arguments");
        return new CallExpr(callee, arguments, typeArgs) { Span = SpanFrom(mark) };
    }

    private Expr ParseArgument()
    {
        var mark = Mark();

        // ECMA-334 §12.6.2 - Output parameters.
        if (Match(TokenType.Out))
            return ParseOutArgument(mark);

        // Named argument: identifier/contextual keyword followed by ':'.
        if (IsParameterName(Peek().Type) && PeekNext().Type == TokenType.Colon)
        {
            var name = Advance();
            Advance(); // colon
            var value = ParseExpression();
            return new NamedArgumentExpr(name, value) { Span = SpanFrom(mark) };
        }

        var argument = ParseExpression();
        return TryLowerImplicitPlaceholderLambda(argument, out var lowered) ? lowered : argument;
    }

    private bool TryLowerImplicitPlaceholderLambda(Expr argument, out Expr lowered)
    {
        lowered = argument;
        if (State.LanguageMode != LanguageMode.Extended || argument is LambdaExpr)
            return false;

        var collector = new IdentifierOccurrenceCollector();
        collector.Collect(argument);

        var foundPlaceholder = false;
        Token placeholderToken = default;
        foreach (var token in collector.GetUnboundTokens(StringComparer.Ordinal))
        {
            if (token.Type != TokenType.Identifier)
                continue;

            if (TokenLexemes.IsImplicitPlaceholderIdentifier(token.Lexeme))
            {
                if (!foundPlaceholder)
                {
                    foundPlaceholder = true;
                    placeholderToken = token;
                }
            }
        }

        if (!foundPlaceholder)
            return false;

        lowered = new LambdaExpr([new LambdaParameter(null, placeholderToken)], argument) { Span = argument.Span };
        return true;
    }

    /// <summary>
    /// Parses an out argument after the 'out' keyword has been consumed.
    /// Handles: out _, out var x, out int x, out SomeType x.
    /// </summary>
    private Expr ParseOutArgument(int mark)
    {
        if (Check(TokenType.Identifier) && string.Equals(Peek().Lexeme, TokenLexemes.DiscardIdentifier, StringComparison.Ordinal))
        {
            Advance();
            return new OutArgExpr(TokenLexemes.DiscardIdentifier, null, true) { Span = SpanFrom(mark) };
        }

        if (MatchVar())
        {
            var varName = ConsumeIdentifierOrContextualKeyword("Expected variable name after 'out var'");
            return new OutArgExpr(varName.Lexeme, null, false) { Span = SpanFrom(mark) };
        }

        string? typeName = TryParseTypeName();
        if (typeName != null && Check(TokenType.Identifier))
        {
            var varName = Advance();
            return new OutArgExpr(varName.Lexeme, typeName, false) { Span = SpanFrom(mark) };
        }

        if (typeName != null)
            return new OutArgExpr(typeName, null, false) { Span = SpanFrom(mark) };

        throw SyntaxError(DiagnosticDescriptors.SyntaxExpected, "variable declaration or discard after 'out'");
    }

    /// <summary>
    /// Returns true if the token type can be used as a parameter name in a named argument.
    /// </summary>
    private static bool IsParameterName(TokenType type)
    {
        return type is TokenType.Identifier or TokenType.Value or TokenType.From or TokenType.Where or TokenType.Select
            or TokenType.Group or TokenType.Into or TokenType.Orderby or TokenType.Join or TokenType.On
            or TokenType.Equals or TokenType.By or TokenType.Ascending or TokenType.Descending or TokenType.Let
            or TokenType.Get or TokenType.Set or TokenType.Add or TokenType.Remove or TokenType.Init or TokenType.When
            or TokenType.With or TokenType.And or TokenType.Or or TokenType.Not or TokenType.File or TokenType.Required
            or TokenType.Scoped or TokenType.Args
            or TokenType.Like or TokenType.Between;
    }
}

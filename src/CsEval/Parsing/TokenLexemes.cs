namespace CsEval.Parsing;

/// <summary>
/// Canonical, single-source lexeme mapping for parser/runtime operator and feature diagnostics.
/// </summary>
internal static class TokenLexemes
{
    internal const string CollectionExpressionLiteral = "[...]";
    internal const string DiscardIdentifier = "_";

    public static string GetCanonical(TokenType type) => type switch
    {
        TokenType.Plus => "+",
        TokenType.Minus => "-",
        TokenType.Star => "*",
        TokenType.Slash => "/",
        TokenType.Percent => "%",
        TokenType.StarStar => "**",

        TokenType.EqualEqual => "==",
        TokenType.BangEqual => "!=",
        TokenType.Less => "<",
        TokenType.LessEqual => "<=",
        TokenType.Greater => ">",
        TokenType.GreaterEqual => ">=",
        TokenType.EqualEqualEqual => "===",
        TokenType.BangEqualEqual => "!==",

        TokenType.AmpAmp => "&&",
        TokenType.PipePipe => "||",
        TokenType.Bang => "!",

        TokenType.Amp => "&",
        TokenType.Pipe => "|",
        TokenType.Caret => "^",
        TokenType.Tilde => "~",
        TokenType.LessLess => "<<",
        TokenType.GreaterGreater => ">>",
        TokenType.GreaterGreaterGreater => ">>>",

        TokenType.Question => "?",
        TokenType.QuestionQuestion => "??",
        TokenType.QuestionQuestionEqual => "??=",
        TokenType.QuestionDot => "?.",
        TokenType.QuestionLeftBracket => "?[",

        TokenType.Equal => "=",
        TokenType.Dot => ".",
        TokenType.DotDot => "..",

        TokenType.PlusEqual => "+=",
        TokenType.MinusEqual => "-=",
        TokenType.StarEqual => "*=",
        TokenType.SlashEqual => "/=",
        TokenType.PercentEqual => "%=",
        TokenType.AmpEqual => "&=",
        TokenType.PipeEqual => "|=",
        TokenType.CaretEqual => "^=",
        TokenType.LessLessEqual => "<<=",
        TokenType.GreaterGreaterEqual => ">>=",
        TokenType.GreaterGreaterGreaterEqual => ">>>=",
        TokenType.StarStarEqual => "**=",

        TokenType.PlusPlus => "++",
        TokenType.MinusMinus => "--",

        TokenType.LeftParen => "(",
        TokenType.RightParen => ")",
        TokenType.LeftBracket => "[",
        TokenType.RightBracket => "]",
        TokenType.LeftBrace => "{",
        TokenType.RightBrace => "}",
        TokenType.Comma => ",",
        TokenType.Colon => ":",
        TokenType.Semicolon => ";",
        TokenType.Arrow => "=>",

        TokenType.And => "and",
        TokenType.Or => "or",
        TokenType.Not => "not",
        TokenType.In => "in",
        TokenType.Like => "like",
        TokenType.Between => "between",
        TokenType.Let => "let",
        TokenType.Const => "const",
        TokenType.Undefined => "undefined",
        TokenType.Unless => "unless",
        TokenType.Until => "until",

        TokenType.PipeGreater => "|>",
        TokenType.EqualTilde => "=~",
        TokenType.BangTilde => "!~",
        TokenType.LessEqualGreater => "<=>",
        TokenType.DotDotLess => "..<",
        TokenType.NotIn => "not in",
        TokenType.NotLike => "not like",
        TokenType.Eof => string.Empty,

        _ => type.ToString()
    };

    public static Token CreateSynthetic(TokenType type, int line, int column, object? literal = null) =>
        new(type, GetCanonical(type), literal, line, column);

    public static Token CreateSynthetic(TokenType type, Token anchor, object? literal = null) =>
        CreateSynthetic(type, anchor.Line, anchor.Column, literal);
}

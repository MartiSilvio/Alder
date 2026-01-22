namespace CsEval.Parsing;

public enum TokenType
{
    // Literals
    Number,
    String,
    InterpolatedString,
    True,
    False,
    Null,

    // Identifiers
    Identifier,

    // Operators - Arithmetic
    Plus,
    Minus,
    Star,
    Slash,
    Percent,

    // Operators - Comparison
    EqualEqual,
    BangEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // Operators - Logical
    AmpAmp,
    PipePipe,
    Bang,

    // Operators - Null/Ternary
    Question,          // ?
    QuestionQuestion,  // ??
    QuestionDot,       // ?.

    // Assignment & Access
    Equal,
    Dot,

    // Delimiters
    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    LeftBrace,
    RightBrace,
    Comma,
    Colon,
    Semicolon,

    // Lambda
    Arrow,  // =>

    // Keywords
    New,
    If,
    Else,
    Switch,
    Case,
    Default,
    Return,
    Var,

    // Special
    Eof
}

public readonly record struct Token(TokenType Type, string Lexeme, object? Literal, int Line, int Column)
{
    public override string ToString() => $"{Type} '{Lexeme}' at {Line}:{Column}";
}
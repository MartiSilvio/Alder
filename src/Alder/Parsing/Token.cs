namespace Alder.Parsing;

internal enum TokenType
{
    // Literals
    Number,
    String,
    Character,
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
    StarStar,          // ** power operator (Extended mode)


    // Operators - Comparison
    EqualEqual,
    BangEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    // JavaScript strict equality (===, !==)
    EqualEqualEqual,   // ===
    BangEqualEqual,    // !==

    // Operators - Logical
    AmpAmp,
    PipePipe,
    Bang,

    // Operators - Bitwise
    Amp,           // &
    Pipe,          // |
    Caret,         // ^
    Tilde,         // ~
    LessLess,      // <<
    GreaterGreater,// >>

    // Operators - Null/Ternary
    Question,              // ?
    QuestionQuestion,      // ??
    QuestionQuestionEqual, // ??=
    QuestionDot,           // ?.
    QuestionLeftBracket,   // ?[

    // Assignment & Access
    Equal,
    Dot,
    DotDot,                // range / spread operator (..)

    // Compound Assignment
    PlusEqual,             // +=
    MinusEqual,            // -=
    StarEqual,             // *=
    SlashEqual,            // /=
    PercentEqual,          // %=
    AmpEqual,              // &=
    PipeEqual,             // |=
    CaretEqual,            // ^=
    LessLessEqual,         // <<=
    GreaterGreaterEqual,   // >>=
    GreaterGreaterGreater,      // >>>
    GreaterGreaterGreaterEqual, // >>>=
    StarStarEqual,             // **= (Extended mode)

    // Increment/Decrement
    PlusPlus,              // ++
    MinusMinus,            // --

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

    #region C# reserved keywords

    // Keywords - Implemented
    New,
    If,
    Else,
    Return,
    Var,

    // JavaScript function keyword (reserved)
    Function,

    // Keywords - Control flow (reserved, not implemented)
    Switch,
    Case,
    Default,
    For,
    Foreach,
    While,
    Do,
    Break,
    Continue,
    Goto,

    // Keywords - Exception handling (reserved, not implemented)
    Try,
    Catch,
    Finally,
    Throw,

    // Keywords - Type declarations (reserved, not implemented)
    Class,
    Struct,
    Interface,
    Enum,
    Record,
    Delegate,
    Namespace,

    // Keywords - Access modifiers (reserved, not implemented)
    Public,
    Private,
    Protected,
    Internal,

    // Keywords - Member modifiers (reserved, not implemented)
    Static,
    Readonly,
    Const,
    Volatile,
    Virtual,
    Override,
    Abstract,
    Sealed,
    Extern,
    Partial,
    Async,
    Await,

    // Keywords - Parameter modifiers (reserved, not implemented)
    Ref,
    Out,
    In,
    Params,

    // Keywords - Type operations (reserved, not implemented)
    Is,
    As,
    Typeof,
    Sizeof,
    Nameof,
    Stackalloc,
    Checked,
    Unchecked,

    // Keywords - Other (reserved, not implemented)
    This,
    Base,
    Super,  // JavaScript super keyword (reserved)
    Using,
    Lock,
    Fixed,
    Unsafe,
    Implicit,
    Explicit,
    Operator,
    Event,

    #endregion

    Int,
    Long,
    Double,
    Float,
    Decimal,
    StringType,  // 'string' keyword (String token is used for string literals)
    Bool,
    Object,
    Void,
    Sbyte,
    Byte,
    Short,
    Ushort,
    Uint,
    Ulong,
    Char,
    Nint,
    Nuint,
    Dynamic,

    Add,
    Alias,
    And,
    Args,
    Ascending,
    By,
    Descending,
    Equals,
    File,
    From,
    Get,
    Global,
    Group,
    Init,
    Into,
    Join,
    Let,
    Managed,
    Not,
    Notnull,
    On,
    Or,
    Orderby,
    Remove,
    Required,
    Scoped,
    Select,
    Set,
    Unmanaged,
    Value,
    When,
    Where,
    With,
    Yield,

    // Extended mode operators (contextual keywords)
    Like,              // like (SQL pattern matching, Extended mode)
    Between,           // between (SQL range check, Extended mode)
    Unless,            // unless (negated if, Extended mode)
    Until,             // until (negated while, Extended mode)

    // Extended mode operators (symbol tokens)
    PipeGreater,           // |> (pipeline operator, Extended mode)
    EqualTilde,            // =~ (regex match, Extended mode)
    BangTilde,             // !~ (negated regex match, Extended mode)
    LessEqualGreater,      // <=> (spaceship operator, Extended mode)
    DotDotLess,            // ..< (exclusive range, Extended mode)
    DotDotEquals,          // ..= (inclusive range, Extended mode)

    // Synthetic tokens (created by parser, not lexer)
    NotIn,             // not in (Extended mode compound keyword operator)
    NotLike,           // not like (Extended mode compound keyword operator)

    // Special
    Eof
}

internal readonly record struct Token(TokenType Type, string Lexeme, object? Literal, int Line, int Column, int Start = 0)
{
    public int Length => Lexeme.Length;
    public Text.TextSpan Span => new(Start, Length);
    public override string ToString() => $"{Type} '{Lexeme}' at {Line}:{Column}";
}

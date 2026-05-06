namespace Alder.Parsing;

internal enum TokenType
{
    Number,
    String,
    Character,
    InterpolatedString,
    True,
    False,
    Null,

    Identifier,

    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    StarStar,          // ** power operator (Extended mode)


    EqualEqual,
    BangEqual,
    Less,
    LessEqual,
    Greater,
    GreaterEqual,

    EqualEqualEqual,   // ===
    BangEqualEqual,    // !==

    AmpAmp,
    PipePipe,
    Bang,

    Amp,           // &
    Pipe,          // |
    Caret,         // ^
    Tilde,         // ~
    LessLess,      // <<
    GreaterGreater,// >>

    Question,              // ?
    QuestionQuestion,      // ??
    QuestionQuestionEqual, // ??=
    QuestionDot,           // ?.
    QuestionLeftBracket,   // ?[

    Equal,
    Dot,
    DotDot,                // range / spread operator (..)

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

    PlusPlus,              // ++
    MinusMinus,            // --

    LeftParen,
    RightParen,
    LeftBracket,
    RightBracket,
    LeftBrace,
    RightBrace,
    Comma,
    Colon,
    Semicolon,

    Arrow,  // =>

    New,
    If,
    Else,
    Return,
    Var,

    Function,

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

    Try,
    Catch,
    Finally,
    Throw,

    Class,
    Struct,
    Interface,
    Enum,
    Record,
    Delegate,
    Namespace,

    Public,
    Private,
    Protected,
    Internal,

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

    Ref,
    Out,
    In,
    Params,

    Is,
    As,
    Typeof,
    Sizeof,
    Nameof,
    Stackalloc,
    Checked,
    Unchecked,

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
    Yield,

    Like,              // like (SQL pattern matching, Extended mode)
    Between,           // between (SQL range check, Extended mode)
    Unless,            // unless (negated if, Extended mode)
    Until,             // until (negated while, Extended mode)

    PipeGreater,           // |> (pipeline operator, Extended mode)
    EqualTilde,            // =~ (regex match, Extended mode)
    BangTilde,             // !~ (negated regex match, Extended mode)
    LessEqualGreater,      // <=> (spaceship operator, Extended mode)
    DotDotLess,            // ..< (exclusive range, Extended mode)
    DotDotEquals,          // ..= (inclusive range, Extended mode)

    // Synthetic tokens created by the parser rather than the lexer.
    NotIn,             // not in (Extended mode compound keyword operator)
    NotLike,           // not like (Extended mode compound keyword operator)

    Eof
}

internal readonly record struct Token(TokenType Type, string Lexeme, object? Literal, int Line, int Column, int Start = 0)
{
    public int Length => Lexeme.Length;
    public Text.TextSpan Span => new(Start, Length);
    public override string ToString() => $"{Type} '{Lexeme}' at {Line}:{Column}";
}

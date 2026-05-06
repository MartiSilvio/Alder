namespace Alder.Diagnostics;

/// <summary>
/// Diagnostic identifiers used by Alder.
/// Roslyn-compatible codes are preserved where Alder mirrors an existing C# error,
/// and Alder-specific codes are used where no Roslyn equivalent exists.
/// </summary>
public enum DiagnosticCode
{
    /// <summary>Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'</summary>
    CS0019 = 19,
    /// <summary>Cannot apply indexing with [] to an expression of type '{0}'</summary>
    CS0021 = 21,
    /// <summary>Operator '{0}' cannot be applied to operand of type '{1}'</summary>
    CS0023 = 23,

    /// <summary>Cannot implicitly convert type '{0}' to '{1}'</summary>
    CS0029 = 29,
    /// <summary>Cannot convert type '{0}' to '{1}'</summary>
    CS0030 = 30,
    /// <summary>Constant value '{0}' cannot be converted to a '{1}'</summary>
    CS0031 = 31,
    /// <summary>Cannot convert null to '{0}' because it is a non-nullable value type</summary>
    CS0037 = 37,

    /// <summary>The name '{0}' does not exist in the current context</summary>
    CS0103 = 103,
    /// <summary>'{0}' is an ambiguous reference between '{1}' and '{2}'</summary>
    CS0104 = 104,
    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS0117 = 117,
    /// <summary>The call is ambiguous between the following methods or properties: '{0}' and '{1}'</summary>
    CS0121 = 121,
    /// <summary>No overload for '{0}' matches delegate '{1}'</summary>
    CS0123 = 123,
    /// <summary>A local variable or function named '{0}' is already defined in this scope</summary>
    CS0128 = 128,
    /// <summary>The left-hand side of an assignment must be a variable, property or indexer</summary>
    CS0131 = 131,
    /// <summary>The label '{0}' is a duplicate</summary>
    CS0152 = 152,
    /// <summary>The expression being assigned to '{0}' must be constant</summary>
    CS0133 = 133,
    /// <summary>Cannot create an instance of the abstract type or interface '{0}'</summary>
    CS0144 = 144,
    /// <summary>Cannot create an instance of the static class '{0}'</summary>
    CS0712 = 712,
    /// <summary>A constant value is expected</summary>
    CS0150 = 150,
    /// <summary>No enclosing loop out of which to break or continue</summary>
    CS0139 = 139,
    /// <summary>The type caught or thrown must be derived from System.Exception</summary>
    CS0155 = 155,
    /// <summary>A throw statement with no arguments is not allowed outside of a catch clause</summary>
    CS0156 = 156,
    /// <summary>Control cannot leave the body of a finally clause</summary>
    CS0157 = 157,
    /// <summary>No such label '{0}' within the scope of the goto statement</summary>
    CS0159 = 159,
    /// <summary>A previous catch clause already catches all exceptions of this or of a super type</summary>
    CS0160 = 160,
    /// <summary>Control cannot fall through from one case label to another</summary>
    CS0163 = 163,
    /// <summary>A lock expression must be a reference type</summary>
    CS0185 = 185,
    /// <summary>A readonly field cannot be assigned to</summary>
    CS0191 = 191,
    /// <summary>Property or indexer '{0}' cannot be assigned to -- it is read only</summary>
    CS0200 = 200,
    /// <summary>'{0}' does not have a predefined size</summary>
    CS0233 = 233,
    /// <summary>The type or namespace name '{0}' could not be found</summary>
    CS0246 = 246,
    /// <summary>Using the generic type '{0}' requires {1} type arguments</summary>
    CS0305 = 305,
    /// <summary>Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)</summary>
    CS0266 = 266,

    /// <summary>A query body must end with a select clause or a group clause</summary>
    CS0742 = 742,
    /// <summary>Expected contextual keyword '{0}'</summary>
    CS0744 = 744,
    /// <summary>Cannot assign null to an implicitly-typed variable</summary>
    CS0815 = 815,
    /// <summary>Implicitly-typed variables must be initialized</summary>
    CS0818 = 818,
    /// <summary>No best type found for an implicitly-typed array</summary>
    CS0826 = 826,
    /// <summary>An anonymous type cannot have multiple properties with the same name</summary>
    CS0833 = 833,

    /// <summary>; expected</summary>
    CS1002 = 1002,
    /// <summary>Syntax error, '{0}' expected</summary>
    CS1003 = 1003,
    /// <summary>Unrecognized escape sequence</summary>
    CS1009 = 1009,
    /// <summary>Newline in constant</summary>
    CS1010 = 1010,
    /// <summary>Empty character literal</summary>
    CS1011 = 1011,
    /// <summary>Too many characters in character literal</summary>
    CS1012 = 1012,
    /// <summary>Invalid number</summary>
    CS1013 = 1013,
    /// <summary>Try statement already has an empty catch block</summary>
    CS1017 = 1017,
    /// <summary>Expected catch or finally</summary>
    CS1524 = 1524,
    /// <summary>Type expected</summary>
    CS1031 = 1031,
    /// <summary>) expected</summary>
    CS1026 = 1026,
    /// <summary>Integral constant is too large</summary>
    CS1021 = 1021,
    /// <summary>No overload for method '{0}' takes {1} arguments</summary>
    CS1501 = 1501,
    /// <summary>Argument {0}: cannot convert from '{1}' to '{2}'</summary>
    CS1503 = 1503,
    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS1061 = 1061,
    /// <summary>The operand of an increment or decrement operator must be a variable, property or indexer</summary>
    CS1059 = 1059,
    /// <summary>Cannot assign to '{0}' because it is a '{1}'</summary>
    CS1656 = 1656,
    /// <summary>Invalid expression term '{0}'</summary>
    CS1525 = 1525,
    /// <summary>The best overload for '{0}' does not have a parameter named '{1}'</summary>
    CS1739 = 1739,
    /// <summary>foreach requires GetEnumerator</summary>
    CS1579 = 1579,
    /// <summary>Delegate '{0}' does not take {1} arguments</summary>
    CS1593 = 1593,
    /// <summary>Cannot convert lambda expression to type '{0}' because it is not a delegate type</summary>
    CS1660 = 1660,
    /// <summary>Cannot convert lambda to delegate type because parameter types do not match</summary>
    CS1661 = 1661,
    /// <summary>Cannot convert {0} to intended delegate type because some of the return types in the block are not implicitly convertible to the delegate return type</summary>
    CS1662 = 1662,
    /// <summary>'{0}' does not contain a constructor that takes {1} arguments</summary>
    CS1729 = 1729,
    /// <summary>Expression expected</summary>
    CS1733 = 1733,
    /// <summary>Non-invocable member '{0}' cannot be used like a method</summary>
    CS1955 = 1955,
    /// <summary>Cannot await in the body of a lock statement</summary>
    CS1996 = 1996,
    /// <summary>Since '{0}' returns void, a return keyword must not be followed by an object expression</summary>
    CS0127 = 127,
    /// <summary>There is no target type for the default literal.</summary>
    CS8716 = 8716,
    /// <summary>The delegate type could not be inferred.</summary>
    CS8917 = 8917,
    /// <summary>It is not legal to use nullable type '{0}?' in a pattern; use the underlying type '{0}' instead</summary>
    CS8116 = 8116,
    /// <summary>An expression of type '{0}' cannot be handled by a pattern of type '{1}'</summary>
    CS8121 = 8121,
    /// <summary>List patterns may not be used for a value of type '{0}'</summary>
    CS8985 = 8985,

    /// <summary>Cannot await '{0}'</summary>
    CS4001 = 4001,
    /// <summary>An expression tree may not contain '{0}'</summary>
    CS7053 = 7053,
    /// <summary>There is no argument given that corresponds to the required parameter '{0}' of '{1}'</summary>
    CS7036 = 7036,

    /// <summary>An expression is too long or complex to compile</summary>
    CS8078 = 8078,
    /// <summary>Tuple must contain at least two elements</summary>
    CS8124 = 8124,
    /// <summary>No suitable 'Deconstruct' method was found for type '{0}'</summary>
    CS8129 = 8129,
    /// <summary>Cannot deconstruct a tuple of '{0}' elements into '{1}' variables</summary>
    CS8132 = 8132,
    /// <summary>Cannot infer the type of implicitly-typed deconstruction variable '{0}'</summary>
    CS8130 = 8130,
    /// <summary>Expression does not have a name</summary>
    CS8081 = 8081,
    /// <summary>The switch expression does not handle all possible values of its input type</summary>
    CS8509 = 8509,
    /// <summary>The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match</summary>
    CS8510 = 8510,
    /// <summary>Relational patterns may not be used for a value of type '{0}'</summary>
    CS8781 = 8781,
    /// <summary>Cannot convert lambda to delegate type because return type does not match</summary>
    CS8934 = 8934,
    /// <summary>Unterminated raw string literal</summary>
    CS8997 = 8997,
    /// <summary>There is no target type for the collection expression</summary>
    CS9176 = 9176,
    /// <summary>A constant value of type '{0}' is expected</summary>
    CS9135 = 9135,

    /// <summary>Strict compilation mode could not compile the expression to IL</summary>
    ALDR0001 = 1_000_001,
    /// <summary>Expression binding failed</summary>
    ALDR0002 = 1_000_002,
    /// <summary>Compiled expression is stale because variable types have changed since compilation</summary>
    ALDR0003 = 1_000_003,

    /// <summary>ParseAsExpression requires a generic Func-style delegate type</summary>
    ALDR0010 = 1_000_010,
    /// <summary>ParseAsExpression requires lambda input</summary>
    ALDR0011 = 1_000_011,
    /// <summary>Compile parameter count mismatch</summary>
    ALDR0012 = 1_000_012,

    /// <summary>Feature requires LanguageMode.Extended</summary>
    ALDR0020 = 1_000_020,

    /// <summary>Method calls blocked by security policy</summary>
    ALDR0100 = 1_000_100,
    /// <summary>Variable assignment blocked by security policy</summary>
    ALDR0101 = 1_000_101,
    /// <summary>Index assignment blocked by security policy</summary>
    ALDR0102 = 1_000_102,
    /// <summary>Property read blocked by security policy</summary>
    ALDR0103 = 1_000_103,
    /// <summary>Static member access blocked by security policy</summary>
    ALDR0104 = 1_000_104,
    /// <summary>Property assignment blocked by security policy</summary>
    ALDR0105 = 1_000_105,
    /// <summary>Object construction blocked by security policy</summary>
    ALDR0106 = 1_000_106,
    /// <summary>Type blocked by security policy</summary>
    ALDR0107 = 1_000_107,
    /// <summary>Reflection type access blocked</summary>
    ALDR0108 = 1_000_108,

    /// <summary>Statement limit exceeded</summary>
    ALDR0200 = 1_000_200,
    /// <summary>Timeout exceeded</summary>
    ALDR0201 = 1_000_201,
    /// <summary>Collection size exceeded</summary>
    ALDR0202 = 1_000_202,
    /// <summary>Loop iteration limit exceeded</summary>
    ALDR0203 = 1_000_203,

    /// <summary>Cannot access member on null</summary>
    ALDR0300 = 1_000_300,
    /// <summary>Cannot call method on null</summary>
    ALDR0301 = 1_000_301,
    /// <summary>Cannot call null as a function</summary>
    ALDR0302 = 1_000_302,
    /// <summary>Cannot assign to property on null</summary>
    ALDR0303 = 1_000_303,
    /// <summary>Method invocation failed</summary>
    ALDR0304 = 1_000_304,
    /// <summary>Multi-parameter indexer not supported</summary>
    ALDR0305 = 1_000_305,
    /// <summary>Unsupported member type</summary>
    ALDR0306 = 1_000_306,
    /// <summary>Indexer access failed at runtime</summary>
    ALDR0307 = 1_000_307,
    /// <summary>Semantic validation failed</summary>
    ALDR0308 = 1_000_308,
    /// <summary>Pattern type not yet implemented</summary>
    ALDR0309 = 1_000_309,
    /// <summary>Unknown relational pattern operator</summary>
    ALDR0310 = 1_000_310,
    /// <summary>Invalid out argument index</summary>
    ALDR0311 = 1_000_311,
    /// <summary>Unsupported tuple arity</summary>
    ALDR0312 = 1_000_312,
    /// <summary>Unsupported delegate arity</summary>
    ALDR0313 = 1_000_313,
    /// <summary>Could not resolve delegate type definition</summary>
    ALDR0314 = 1_000_314,
    /// <summary>Cannot resolve module instance</summary>
    ALDR0315 = 1_000_315,
    /// <summary>Member requires generated dispatch in authoritative generated mode</summary>
    ALDR0316 = 1_000_316,
    /// <summary>Method requires generated dispatch in authoritative generated mode</summary>
    ALDR0317 = 1_000_317,
    /// <summary>Constructor requires generated dispatch in authoritative generated mode</summary>
    ALDR0318 = 1_000_318,

    /// <summary>Cannot slice null</summary>
    ALDR0400 = 1_000_400,
    /// <summary>Slice step cannot be zero</summary>
    ALDR0401 = 1_000_401,
    /// <summary>Cannot slice type '{0}'</summary>
    ALDR0402 = 1_000_402,
    /// <summary>Unsupported compound assignment operator</summary>
    ALDR0403 = 1_000_403,
    /// <summary>Unsupported chained comparison operator</summary>
    ALDR0404 = 1_000_404,
    /// <summary>Spread operator used outside array or object literal</summary>
    ALDR0405 = 1_000_405,
    /// <summary>Cannot materialize projection of type '{0}' to '{1}': {2}</summary>
    ALDR0406 = 1_000_406,
}

internal static class DiagnosticCodeExtensions
{
    internal static string ToDiagnosticId(this DiagnosticCode code)
    {
        var value = (int)code;
        if (value >= 1_000_000)
            return $"ALDR{value - 1_000_000:D4}";
        return $"CS{value:D4}";
    }
}

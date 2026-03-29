namespace Alder.Diagnostics;

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
    /// <summary>Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)</summary>
    CS0266 = 266,

    /// <summary>The name '{0}' does not exist in the current context</summary>
    CS0103 = 103,
    /// <summary>'{0}' is an ambiguous reference between '{1}' and '{2}'</summary>
    CS0104 = 104,
    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS0117 = 117,
    /// <summary>The type or namespace name '{0}' could not be found</summary>
    CS0246 = 246,

    /// <summary>The call is ambiguous between the following methods or properties: '{0}' and '{1}'</summary>
    CS0121 = 121,
    /// <summary>No overload for '{0}' matches delegate '{1}'</summary>
    CS0123 = 123,
    /// <summary>No overload for method '{0}' takes {1} arguments</summary>
    CS1501 = 1501,
    /// <summary>Cannot convert lambda to delegate type — parameter types do not match</summary>
    CS1661 = 1661,
    /// <summary>Non-invocable member '{0}' cannot be used like a method</summary>
    CS1955 = 1955,
    /// <summary>There is no argument given that corresponds to the required parameter '{0}' of '{1}'</summary>
    CS7036 = 7036,
    /// <summary>Cannot convert lambda to delegate type — return type does not match</summary>
    CS8934 = 8934,

    /// <summary>A local variable or function named '{0}' is already defined in this scope</summary>
    CS0128 = 128,
    /// <summary>The left-hand side of an assignment must be a variable, property or indexer</summary>
    CS0131 = 131,
    /// <summary>A readonly field cannot be assigned to</summary>
    CS0191 = 191,
    /// <summary>Cannot assign null to an implicitly-typed variable</summary>
    CS0815 = 815,
    /// <summary>No best type found for an implicitly-typed array</summary>
    CS0826 = 826,
    /// <summary>There is no target type for the collection expression</summary>
    CS9176 = 9176,

    /// <summary>No enclosing loop out of which to break or continue</summary>
    CS0139 = 139,
    /// <summary>The type caught or thrown must be derived from System.Exception</summary>
    CS0155 = 155,
    /// <summary>A throw statement with no arguments is not allowed outside of a catch clause</summary>
    CS0156 = 156,
    /// <summary>No such label '{0}' within the scope of the goto statement</summary>
    CS0159 = 159,
    /// <summary>Control cannot fall through from one case label to another</summary>
    CS0163 = 163,
    /// <summary>A lock expression must be a reference type</summary>
    CS0185 = 185,

    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS1061 = 1061,
    /// <summary>foreach requires GetEnumerator</summary>
    CS1579 = 1579,
    /// <summary>'{0}' does not contain a constructor that takes {1} arguments</summary>
    CS1729 = 1729,

    /// <summary>Syntax error, '{0}' expected</summary>
    CS1003 = 1003,
    /// <summary>Invalid number</summary>
    CS1013 = 1013,
    /// <summary>Try statement already has an empty catch block</summary>
    CS1017 = 1017,
    /// <summary>Integral constant is too large</summary>
    CS1021 = 1021,
    /// <summary>Invalid expression term '{0}'</summary>
    CS1525 = 1525,
    /// <summary>Expression expected</summary>
    CS1733 = 1733,

    /// <summary>A query body must end with a select clause or a group clause</summary>
    CS0742 = 742,
    /// <summary>Expected contextual keyword '{0}'</summary>
    CS0744 = 744,

    /// <summary>'{0}' does not have a predefined size</summary>
    CS0233 = 233,

    /// <summary>An expression tree may not contain '{0}'</summary>
    CS7053 = 7053,

    /// <summary>An expression is too long or complex to compile</summary>
    CS8078 = 8078,
    /// <summary>Tuple must contain at least two elements</summary>
    CS8124 = 8124,
    /// <summary>No suitable 'Deconstruct' method was found for type '{0}'</summary>
    CS8129 = 8129,
    /// <summary>Cannot deconstruct a tuple of '{0}' elements into '{1}' variables</summary>
    CS8132 = 8132,

    /// <summary>The pattern is unreachable</summary>
    CS8510 = 8510,
    /// <summary>Unterminated raw string literal</summary>
    CS8997 = 8997,

    /// <summary>Strict compilation mode could not compile the expression to IL</summary>
    ALDR0001 = 1_000_001,
    /// <summary>Expression binding failed</summary>
    ALDR0002 = 1_000_002,
    /// <summary>Call requires runtime overload resolution</summary>
    ALDR0003 = 1_000_003,

    /// <summary>ParseAsExpression requires a generic Func-style delegate type</summary>
    ALDR0010 = 1_000_010,
    /// <summary>ParseAsExpression requires lambda input</summary>
    ALDR0011 = 1_000_011,

    /// <summary>Feature requires LanguageMode.Extended</summary>
    ALDR0020 = 1_000_020,

    /// <summary>Method calls blocked by sandbox</summary>
    ALDR0100 = 1_000_100,
    /// <summary>Variable assignment blocked by sandbox</summary>
    ALDR0101 = 1_000_101,
    /// <summary>Index assignment blocked by sandbox</summary>
    ALDR0102 = 1_000_102,
    /// <summary>Property read blocked by sandbox</summary>
    ALDR0103 = 1_000_103,
    /// <summary>Static member access blocked by sandbox</summary>
    ALDR0104 = 1_000_104,
    /// <summary>Property assignment blocked by sandbox</summary>
    ALDR0105 = 1_000_105,
    /// <summary>Object construction blocked by sandbox</summary>
    ALDR0106 = 1_000_106,
    /// <summary>Type blocked by sandbox</summary>
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

    /// <summary>Type requires AOT registration for NativeAOT environments</summary>
    ALDR0500 = 1_000_500,
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

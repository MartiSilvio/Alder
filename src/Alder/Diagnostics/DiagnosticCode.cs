namespace Alder.Diagnostics;

public enum DiagnosticCode
{
    // ── CS codes (Roslyn parity) ─────────────────────────────────────

    // Operators
    CS0019 = 19,       // Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'
    CS0021 = 21,       // Cannot apply indexing with [] to an expression of type '{0}'
    CS0023 = 23,       // Operator '{0}' cannot be applied to operand of type '{1}'

    // Type conversion
    CS0029 = 29,       // Cannot implicitly convert type '{0}' to '{1}'
    CS0030 = 30,       // Cannot convert type '{0}' to '{1}'
    CS0031 = 31,       // Constant value '{0}' cannot be converted to a '{1}'
    CS0037 = 37,       // Cannot convert null to '{0}' because it is a non-nullable value type
    CS0266 = 266,      // Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)

    // Name resolution
    CS0103 = 103,      // The name '{0}' does not exist in the current context
    CS0104 = 104,      // '{0}' is an ambiguous reference between '{1}' and '{2}'
    CS0117 = 117,      // '{0}' does not contain a definition for '{1}'
    CS0246 = 246,      // The type or namespace name '{0}' could not be found

    // Method resolution
    CS0121 = 121,      // The call is ambiguous between the following methods or properties: '{0}' and '{1}'
    CS0123 = 123,      // No overload for '{0}' matches delegate '{1}'
    CS1501 = 1501,     // No overload for method '{0}' takes {1} arguments
    CS1661 = 1661,     // Cannot convert lambda to delegate type — parameter types do not match
    CS1955 = 1955,     // Non-invocable member '{0}' cannot be used like a method
    CS7036 = 7036,     // There is no argument given that corresponds to the required parameter '{0}' of '{1}'
    CS8934 = 8934,     // Cannot convert lambda to delegate type — return type does not match

    // Variables and assignment
    CS0128 = 128,      // A local variable or function named '{0}' is already defined in this scope
    CS0131 = 131,      // The left-hand side of an assignment must be a variable, property or indexer
    CS0191 = 191,      // A readonly field cannot be assigned to
    CS0815 = 815,      // Cannot assign null to an implicitly-typed variable

    // Control flow
    CS0139 = 139,      // No enclosing loop out of which to break or continue
    CS0155 = 155,      // The type caught or thrown must be derived from System.Exception
    CS0156 = 156,      // A throw statement with no arguments is not allowed outside of a catch clause
    CS0159 = 159,      // No such label '{0}' within the scope of the goto statement
    CS0163 = 163,      // Control cannot fall through from one case label to another
    CS0185 = 185,      // A lock expression must be a reference type

    // Constructors and member access
    CS1061 = 1061,     // '{0}' does not contain a definition for '{1}'
    CS1579 = 1579,     // foreach requires GetEnumerator
    CS1729 = 1729,     // '{0}' does not contain a constructor that takes {1} arguments

    // Syntax and parsing
    CS1003 = 1003,     // Syntax error, '{0}' expected
    CS1013 = 1013,     // Invalid number
    CS1017 = 1017,     // Try statement already has an empty catch block
    CS1021 = 1021,     // Integral constant is too large
    CS1525 = 1525,     // Invalid expression term '{0}'
    CS1733 = 1733,     // Expression expected

    // LINQ and queries
    CS0742 = 742,      // A query body must end with a select clause or a group clause
    CS0744 = 744,      // Expected contextual keyword '{0}'

    // Sizeof
    CS0233 = 233,      // '{0}' does not have a predefined size

    // Expression trees
    CS7053 = 7053,     // An expression tree may not contain '{0}'

    // Tuples and deconstruction
    CS8078 = 8078,     // An expression is too long or complex to compile
    CS8124 = 8124,     // Tuple must contain at least two elements
    CS8129 = 8129,     // No suitable 'Deconstruct' method was found for type '{0}'
    CS8132 = 8132,     // Cannot deconstruct a tuple of '{0}' elements into '{1}' variables

    // Pattern matching
    CS8510 = 8510,     // The pattern is unreachable

    // ── ALDR codes (Alder-specific) ──────────────────────────────────

    // Compilation
    ALDR0001 = 1_000_001, // Strict compilation mode could not compile the expression to IL
    ALDR0002 = 1_000_002, // Expression binding failed
    ALDR0003 = 1_000_003, // Call requires runtime overload resolution

    // ParseAsExpression API
    ALDR0010 = 1_000_010, // ParseAsExpression requires a generic Func-style delegate type
    ALDR0011 = 1_000_011, // ParseAsExpression requires lambda input

    // Language mode
    ALDR0020 = 1_000_020, // Feature requires LanguageMode.Extended

    // Sandbox
    ALDR0100 = 1_000_100, // Method calls blocked by sandbox
    ALDR0101 = 1_000_101, // Variable assignment blocked by sandbox
    ALDR0102 = 1_000_102, // Index assignment blocked by sandbox
    ALDR0103 = 1_000_103, // Property read blocked by sandbox
    ALDR0104 = 1_000_104, // Static member access blocked by sandbox
    ALDR0105 = 1_000_105, // Property assignment blocked by sandbox
    ALDR0106 = 1_000_106, // Object construction blocked by sandbox
    ALDR0107 = 1_000_107, // Type not in sandbox allowlist
    ALDR0108 = 1_000_108, // Reflection type access blocked

    // Execution constraints
    ALDR0200 = 1_000_200, // Expression depth exceeded
    ALDR0201 = 1_000_201, // Statement limit exceeded
    ALDR0202 = 1_000_202, // Timeout exceeded
    ALDR0203 = 1_000_203, // Array length exceeded

    // Runtime
    ALDR0300 = 1_000_300, // Cannot access member on null
    ALDR0301 = 1_000_301, // Cannot call method on null
    ALDR0302 = 1_000_302, // Cannot call null as a function
    ALDR0303 = 1_000_303, // Cannot assign to property on null
    ALDR0304 = 1_000_304, // Method invocation failed
    ALDR0305 = 1_000_305, // Multi-parameter indexer not supported
    ALDR0306 = 1_000_306, // Unsupported member type
    ALDR0307 = 1_000_307, // Indexer access failed at runtime
    ALDR0308 = 1_000_308, // Semantic validation failed
    ALDR0309 = 1_000_309, // Pattern type not yet implemented
    ALDR0310 = 1_000_310, // Unknown relational pattern operator
    ALDR0311 = 1_000_311, // Invalid out argument index
    ALDR0312 = 1_000_312, // Unsupported tuple arity
    ALDR0313 = 1_000_313, // Unsupported delegate arity
    ALDR0314 = 1_000_314, // Could not resolve delegate type definition
    ALDR0315 = 1_000_315, // Cannot resolve module instance

    // Extended mode
    ALDR0400 = 1_000_400, // Cannot slice null
    ALDR0401 = 1_000_401, // Slice step cannot be zero
    ALDR0402 = 1_000_402, // Cannot slice type '{0}'
    ALDR0403 = 1_000_403, // Unsupported compound assignment operator
    ALDR0404 = 1_000_404, // Unsupported chained comparison operator
    ALDR0405 = 1_000_405, // Spread operator used outside array or object literal
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

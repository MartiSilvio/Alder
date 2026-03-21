namespace Alder.Diagnostics;

/// <summary>
/// Diagnostic error codes. CS#### codes match Roslyn for parity.
/// CSEV#### codes are Alder-specific (no Roslyn equivalent).
/// </summary>
public enum DiagnosticCode
{
    // ── CS codes (Roslyn parity) ─────────────────────────────────────

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

    /// <summary>'{0}' is not a delegate type; cannot convert to delegate</summary>
    CS0123 = 123,

    /// <summary>A local variable or function named '{0}' is already defined in this scope</summary>
    CS0128 = 128,

    /// <summary>The left-hand side of an assignment must be a variable, property or indexer</summary>
    CS0131 = 131,

    /// <summary>No enclosing loop out of which to break or continue</summary>
    CS0139 = 139,

    /// <summary>The type caught or thrown must be derived from System.Exception</summary>
    CS0155 = 155,

    /// <summary>A throw statement with no arguments is not allowed outside of a catch clause</summary>
    CS0156 = 156,

    /// <summary>Control cannot fall through from one case label to another</summary>
    CS0163 = 163,

    /// <summary>A lock expression must be a reference type</summary>
    CS0185 = 185,

    /// <summary>A readonly field cannot be assigned to</summary>
    CS0191 = 191,

    /// <summary>'{0}' does not have a predefined size, therefore sizeof can only be used in an unsafe context</summary>
    CS0233 = 233,

    /// <summary>The type or namespace name '{0}' could not be found</summary>
    CS0246 = 246,

    /// <summary>Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)</summary>
    CS0266 = 266,

    /// <summary>A query body must end with a select clause or a group clause</summary>
    CS0742 = 742,

    /// <summary>Expected contextual keyword '{0}'</summary>
    CS0744 = 744,

    /// <summary>Cannot assign null to an implicitly-typed variable</summary>
    CS0815 = 815,

    /// <summary>Syntax error, '{0}' expected</summary>
    CS1003 = 1003,

    /// <summary>Invalid number</summary>
    CS1013 = 1013,

    /// <summary>Try statement already has an empty catch block</summary>
    CS1017 = 1017,

    /// <summary>Integral constant is too large</summary>
    CS1021 = 1021,

    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS1061 = 1061,

    /// <summary>No overload for method '{0}' takes {1} arguments</summary>
    CS1501 = 1501,

    /// <summary>Invalid expression term '{0}'</summary>
    CS1525 = 1525,

    /// <summary>foreach requires GetEnumerator</summary>
    CS1579 = 1579,

    /// <summary>'{0}' does not contain a constructor that takes {1} arguments</summary>
    CS1729 = 1729,

    /// <summary>Expression expected</summary>
    CS1733 = 1733,

    /// <summary>Non-invocable member '{0}' cannot be used like a method</summary>
    CS1955 = 1955,

    /// <summary>There is no argument given that corresponds to the required parameter '{0}' of '{1}'</summary>
    CS7036 = 7036,

    /// <summary>Tuple must contain at least two elements</summary>
    CS8124 = 8124,

    /// <summary>No suitable 'Deconstruct' instance or extension method was found for type '{0}'</summary>
    CS8129 = 8129,

    /// <summary>Deconstruction must contain at least two variables</summary>
    CS8132 = 8132,

    /// <summary>The pattern is unreachable. It has already been handled by a previous arm of the switch expression or it is impossible to match.</summary>
    CS8510 = 8510,

    // ── CSEV codes (Alder-specific) ─────────────────────────────────

    // Compilation and expression tree
    /// <summary>Strict compilation mode could not compile the expression to IL.</summary>
    CSEV0001 = 1_000_001,
    /// <summary>Expression tree output does not support the requested node or construct.</summary>
    CSEV0002 = 1_000_002,
    /// <summary>Expression tree output does not support the requested call shape.</summary>
    CSEV0003 = 1_000_003,
    /// <summary>ParseAsExpression requires a generic Func-style delegate type.</summary>
    CSEV0004 = 1_000_004,
    /// <summary>ParseAsExpression requires lambda input.</summary>
    CSEV0005 = 1_000_005,
    /// <summary>ParseAsExpression lambda parameter count mismatch.</summary>
    CSEV0006 = 1_000_006,
    /// <summary>ParseAsExpression could not convert body to requested return type.</summary>
    CSEV0007 = 1_000_007,
    /// <summary>Expression binding failed.</summary>
    CSEV0008 = 1_000_008,

    // Language mode
    /// <summary>Feature requires LanguageMode.Extended.</summary>
    CSEV0009 = 1_000_009,

    // Sandbox and security
    /// <summary>Method calls blocked by sandbox.</summary>
    CSEV0011 = 1_000_011,
    /// <summary>Sandbox blocked assignment.</summary>
    CSEV0012 = 1_000_012,
    /// <summary>Sandbox blocked index assignment.</summary>
    CSEV0013 = 1_000_013,
    /// <summary>Property access blocked by sandbox.</summary>
    CSEV0014 = 1_000_014,
    /// <summary>Static field access blocked by sandbox.</summary>
    CSEV0015 = 1_000_015,
    /// <summary>Static property access blocked by sandbox.</summary>
    CSEV0016 = 1_000_016,
    /// <summary>Property assignment blocked by sandbox.</summary>
    CSEV0017 = 1_000_017,
    /// <summary>Object construction blocked by sandbox.</summary>
    CSEV0018 = 1_000_018,
    /// <summary>Type not in sandbox allowlist.</summary>
    CSEV0019 = 1_000_019,

    // Null access
    /// <summary>Cannot access member on null.</summary>
    CSEV0020 = 1_000_020,
    /// <summary>Cannot call method on null.</summary>
    CSEV0021 = 1_000_021,
    /// <summary>Cannot call null as a function.</summary>
    CSEV0022 = 1_000_022,
    /// <summary>Cannot assign to property on null.</summary>
    CSEV0023 = 1_000_023,

    // Method resolution
    /// <summary>Method invocation failed.</summary>
    CSEV0024 = 1_000_024,
    /// <summary>Call requires runtime overload resolution.</summary>
    CSEV0025 = 1_000_025,

    // Member access and property resolution
    /// <summary>Indexer overloads with more than one parameter are not supported yet.</summary>
    CSEV0026 = 1_000_026,
    /// <summary>Unsupported member type encountered.</summary>
    CSEV0027 = 1_000_027,
    /// <summary>Indexer access failed at runtime.</summary>
    CSEV0028 = 1_000_028,
    /// <summary>No indexer found on type.</summary>
    CSEV0029 = 1_000_029,

    // Type system
    /// <summary>Reflection type access blocked.</summary>
    CSEV0030 = 1_000_030,
    /// <summary>Collection initializer requires Add method.</summary>
    CSEV0031 = 1_000_031,

    // Control flow and semantics
    /// <summary>Semantic validation failed.</summary>
    CSEV0032 = 1_000_032,
    /// <summary>Expression nesting depth exceeded available stack space.</summary>
    CSEV0033 = 1_000_033,
    /// <summary>Cannot slice null.</summary>
    CSEV0034 = 1_000_034,
    /// <summary>Slice step cannot be zero.</summary>
    CSEV0035 = 1_000_035,
    /// <summary>Cannot slice non-sliceable type.</summary>
    CSEV0036 = 1_000_036,
    /// <summary>Unknown compound assignment operator.</summary>
    CSEV0037 = 1_000_037,
    /// <summary>Unsupported compound assignment base operator.</summary>
    CSEV0038 = 1_000_038,
    /// <summary>Unsupported chained comparison operator.</summary>
    CSEV0039 = 1_000_039,
    /// <summary>Spread operator used outside array or object literal.</summary>
    CSEV0040 = 1_000_040,
    /// <summary>goto case/default target not found.</summary>
    CSEV0041 = 1_000_041,
    /// <summary>Pattern type not yet implemented.</summary>
    CSEV0042 = 1_000_042,
    /// <summary>Invalid out argument index.</summary>
    CSEV0043 = 1_000_043,
    /// <summary>Unknown relational pattern operator.</summary>
    CSEV0044 = 1_000_044,
    /// <summary>Unsupported tuple arity.</summary>
    CSEV0045 = 1_000_045,
    /// <summary>Sequence contains no elements.</summary>
    CSEV0046 = 1_000_046,
    /// <summary>Unsupported delegate arity.</summary>
    CSEV0047 = 1_000_047,
    /// <summary>Could not resolve delegate type definition.</summary>
    CSEV0048 = 1_000_048,
    /// <summary>Cannot resolve module instance.</summary>
    CSEV0049 = 1_000_049,
}

internal static class DiagnosticCodeExtensions
{
    internal static string ToDiagnosticId(this DiagnosticCode code)
    {
        var value = (int)code;
        if (value >= 1_000_000)
            return $"CSEV{value - 1_000_000:D4}";
        return $"CS{value:D4}";
    }
}

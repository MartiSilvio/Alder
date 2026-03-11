namespace CsEval.Diagnostics;

/// <summary>
/// C# compiler error codes (CS####) that CsEval maps to for parity with Roslyn diagnostics.
/// Each enum member's integer value matches the CS number.
/// </summary>
public enum DiagnosticCode
{
    // ECMA-334 operator applicability

    /// <summary>Operator '{0}' cannot be applied to operands of type '{1}' and '{2}'</summary>
    CS0019 = 19,

    /// <summary>Cannot apply indexing with [] to an expression of type '{0}'</summary>
    CS0021 = 21,

    /// <summary>Operator '{0}' cannot be applied to operand of type '{1}'</summary>
    CS0023 = 23,

    // ECMA-334 type conversion

    /// <summary>Cannot implicitly convert type '{0}' to '{1}'</summary>
    CS0029 = 29,

    /// <summary>Cannot convert type '{0}' to '{1}'</summary>
    CS0030 = 30,

    /// <summary>Constant value '{0}' cannot be converted to a '{1}'</summary>
    CS0031 = 31,

    /// <summary>Cannot convert null to '{0}' because it is a non-nullable value type</summary>
    CS0037 = 37,

    // ECMA-334 name resolution

    /// <summary>The name '{0}' does not exist in the current context</summary>
    CS0103 = 103,

    /// <summary>'{0}' is an ambiguous reference between '{1}' and '{2}'</summary>
    CS0104 = 104,

    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS0117 = 117,

    /// <summary>A local variable or function named '{0}' is already defined in this scope</summary>
    CS0128 = 128,

    // ECMA-334 control flow

    /// <summary>No enclosing loop out of which to break or continue</summary>
    CS0139 = 139,

    /// <summary>The type caught or thrown must be derived from System.Exception</summary>
    CS0155 = 155,

    /// <summary>A throw statement with no arguments is not allowed outside of a catch clause</summary>
    CS0156 = 156,

    /// <summary>Control cannot fall through from one case label to another</summary>
    CS0163 = 163,

    // ECMA-334 assignment

    /// <summary>A readonly field cannot be assigned to</summary>
    CS0191 = 191,

    // ECMA-334 type/namespace resolution

    /// <summary>The type or namespace name '{0}' could not be found (are you missing a using directive or an assembly reference?)</summary>
    CS0246 = 246,

    /// <summary>Cannot implicitly convert type '{0}' to '{1}'. An explicit conversion exists (are you missing a cast?)</summary>
    CS0266 = 266,

    // ECMA-334 null and implicit typing

    /// <summary>Cannot assign null to an implicitly-typed variable</summary>
    CS0815 = 815,

    // ECMA-334 exception handling

    /// <summary>Try statement already has an empty catch block</summary>
    CS1017 = 1017,

    /// <summary>Integral constant is too large</summary>
    CS1021 = 1021,

    // ECMA-334 member resolution

    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS1061 = 1061,

    // ECMA-334 iteration

    /// <summary>foreach statement cannot operate on variables of type '{0}' because '{0}' does not contain a public instance or extension definition for 'GetEnumerator'</summary>
    CS1579 = 1579,

    // ECMA-334 constructor resolution

    /// <summary>'{0}' does not contain a constructor that takes {1} arguments</summary>
    CS1729 = 1729,

    /// <summary>There is no argument given that corresponds to the required parameter '{0}' of '{1}'</summary>
    CS7036 = 7036,

    // CsEval-local diagnostics (no direct Roslyn equivalent)

    /// <summary>Strict compilation mode could not compile the expression to IL.</summary>
    CSEV0001 = 1_000_001,

    /// <summary>Feature requires LanguageMode.Extended.</summary>
    CSEV0002 = 1_000_002,

    /// <summary>Indexer overloads with more than one parameter are not supported yet.</summary>
    CSEV0003 = 1_000_003,

    /// <summary>Expression tree output does not support the requested node or construct.</summary>
    CSEV0004 = 1_000_004,

    /// <summary>Expression tree output does not support the requested call shape.</summary>
    CSEV0005 = 1_000_005,

    /// <summary>ParseAsExpression requires a generic Func-style delegate type.</summary>
    CSEV0006 = 1_000_006,

    /// <summary>ParseAsExpression requires lambda input.</summary>
    CSEV0007 = 1_000_007,

    /// <summary>ParseAsExpression lambda parameter count mismatch.</summary>
    CSEV0008 = 1_000_008,

    /// <summary>ParseAsExpression could not convert body to requested return type.</summary>
    CSEV0009 = 1_000_009,

    /// <summary>Semantic validation failed.</summary>
    CSEV0010 = 1_000_010,

    /// <summary>Expression nesting depth exceeded available stack space.</summary>
    CSEV0011 = 1_000_011,
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

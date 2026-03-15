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

    /// <summary>The left-hand side of an assignment must be a variable, property or indexer</summary>
    CS0131 = 131,

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

    // CSEV01xx — Compilation and expression tree

    /// <summary>Strict compilation mode could not compile the expression to IL.</summary>
    CSEV0100 = 1_000_100,

    /// <summary>Expression tree output does not support the requested node or construct.</summary>
    CSEV0101 = 1_000_101,

    /// <summary>Expression tree output does not support the requested call shape.</summary>
    CSEV0102 = 1_000_102,

    /// <summary>ParseAsExpression requires a generic Func-style delegate type.</summary>
    CSEV0103 = 1_000_103,

    /// <summary>ParseAsExpression requires lambda input.</summary>
    CSEV0104 = 1_000_104,

    /// <summary>ParseAsExpression lambda parameter count mismatch.</summary>
    CSEV0105 = 1_000_105,

    /// <summary>ParseAsExpression could not convert body to requested return type.</summary>
    CSEV0106 = 1_000_106,

    /// <summary>Expression binding failed.</summary>
    CSEV0107 = 1_000_107,

    /// <summary>Delegate type conversion failed.</summary>
    CSEV0108 = 1_000_108,

    // CSEV02xx — Language mode and parsing

    /// <summary>Feature requires LanguageMode.Extended.</summary>
    CSEV0200 = 1_000_200,

    // CSEV03xx — Sandbox and security

    /// <summary>Sandbox blocked member access.</summary>
    CSEV0300 = 1_000_300,

    /// <summary>Method calls blocked by sandbox.</summary>
    CSEV0301 = 1_000_301,

    /// <summary>Sandbox blocked assignment.</summary>
    CSEV0302 = 1_000_302,

    /// <summary>Sandbox blocked index assignment.</summary>
    CSEV0303 = 1_000_303,

    /// <summary>Property access blocked by sandbox.</summary>
    CSEV0304 = 1_000_304,

    /// <summary>Static field access blocked by sandbox.</summary>
    CSEV0305 = 1_000_305,

    /// <summary>Static property access blocked by sandbox.</summary>
    CSEV0306 = 1_000_306,

    /// <summary>Property assignment blocked by sandbox.</summary>
    CSEV0307 = 1_000_307,

    // CSEV04xx — AOT and trimming

    /// <summary>Type must be registered for NativeAOT compatibility.</summary>
    CSEV0400 = 1_000_400,

    /// <summary>Method call on unregistered type requires NativeAOT registration.</summary>
    CSEV0401 = 1_000_401,

    // CSEV05xx — Null access and null safety

    /// <summary>Cannot access member on null.</summary>
    CSEV0500 = 1_000_500,

    /// <summary>Cannot call method on null.</summary>
    CSEV0501 = 1_000_501,

    /// <summary>Cannot call null as a function.</summary>
    CSEV0502 = 1_000_502,

    /// <summary>Cannot assign to property on null.</summary>
    CSEV0503 = 1_000_503,

    // CSEV06xx — Method resolution and invocation

    /// <summary>Cannot call non-callable type as a function.</summary>
    CSEV0600 = 1_000_600,

    /// <summary>Method invocation failed.</summary>
    CSEV0601 = 1_000_601,

    /// <summary>Ambiguous method invocation.</summary>
    CSEV0602 = 1_000_602,

    /// <summary>Call requires runtime overload resolution.</summary>
    CSEV0603 = 1_000_603,

    /// <summary>No applicable overload found for method.</summary>
    CSEV0604 = 1_000_604,

    // CSEV07xx — Member access and property resolution

    /// <summary>Indexer overloads with more than one parameter are not supported yet.</summary>
    CSEV0700 = 1_000_700,

    /// <summary>Unsupported member type encountered.</summary>
    CSEV0701 = 1_000_701,

    /// <summary>Indexer access failed at runtime.</summary>
    CSEV0702 = 1_000_702,

    /// <summary>No indexer found on type.</summary>
    CSEV0703 = 1_000_703,

    // CSEV08xx — Type system and conversion

    /// <summary>Cannot take sizeof of non-primitive type.</summary>
    CSEV0800 = 1_000_800,

    /// <summary>Reflection type access blocked.</summary>
    CSEV0801 = 1_000_801,

    /// <summary>Cannot deconstruct value.</summary>
    CSEV0802 = 1_000_802,

    /// <summary>Tuples must have at least 2 elements.</summary>
    CSEV0803 = 1_000_803,

    /// <summary>Deconstruction element count mismatch.</summary>
    CSEV0804 = 1_000_804,

    /// <summary>Collection initializer requires Add method.</summary>
    CSEV0805 = 1_000_805,

    // CSEV09xx — Control flow and semantics

    /// <summary>Semantic validation failed.</summary>
    CSEV0900 = 1_000_900,

    /// <summary>Expression nesting depth exceeded available stack space.</summary>
    CSEV0901 = 1_000_901,

    /// <summary>Cannot slice null.</summary>
    CSEV0902 = 1_000_902,

    /// <summary>Slice step cannot be zero.</summary>
    CSEV0903 = 1_000_903,

    /// <summary>Cannot slice non-sliceable type.</summary>
    CSEV0904 = 1_000_904,

    /// <summary>Unknown compound assignment operator.</summary>
    CSEV0905 = 1_000_905,

    /// <summary>Unsupported compound assignment base operator.</summary>
    CSEV0906 = 1_000_906,

    /// <summary>Unsupported chained comparison operator.</summary>
    CSEV0907 = 1_000_907,

    /// <summary>Spread operator used outside array or object literal.</summary>
    CSEV0908 = 1_000_908,

    /// <summary>lock statement requires a non-null reference.</summary>
    CSEV0909 = 1_000_909,

    /// <summary>goto case/default target not found.</summary>
    CSEV0910 = 1_000_910,

    /// <summary>Pattern type not yet implemented.</summary>
    CSEV0911 = 1_000_911,

    /// <summary>Invalid out argument index.</summary>
    CSEV0912 = 1_000_912,

    /// <summary>Unknown relational pattern operator.</summary>
    CSEV0913 = 1_000_913,
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

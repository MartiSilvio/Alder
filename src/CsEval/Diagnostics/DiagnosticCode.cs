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

    // ECMA-334 member resolution

    /// <summary>'{0}' does not contain a definition for '{1}'</summary>
    CS1061 = 1061,

    // ECMA-334 iteration

    /// <summary>foreach statement cannot operate on variables of type '{0}' because '{0}' does not contain a public instance or extension definition for 'GetEnumerator'</summary>
    CS1579 = 1579,

    // ECMA-334 constructor resolution

    /// <summary>'{0}' does not contain a constructor that takes {1} arguments</summary>
    CS1729 = 1729,
}

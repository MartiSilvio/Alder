using NUnit.Framework;

namespace CsEval.TestData.Data;

/// <summary>
/// ECMA-334 S12.12.12 -- The is operator, S12.12.13 -- The as operator,
/// S11.2 -- Pattern matching (type patterns, null patterns).
/// Test data for is/is not type checking, as operator, and pattern matching with variables.
/// Shared across compiler backends.
/// </summary>
public static class IsAsData
{
    /// <summary>
    /// Value-producing is-type expressions with expected results.
    /// Signature: (string expr, bool expected)
    /// </summary>
    public static IEnumerable<TestCaseData> IsTypeCases() =>
    [
        // ECMA-334 S12.12.12 -- Is Operator (Type Checking)
        new("42 is int", true) { TestName = "Is_IntIsInt" },
        new("42L is long", true) { TestName = "Is_LongIsLong" },
        new("3.14 is double", true) { TestName = "Is_DoubleIsDouble" },
        new("3.14f is float", true) { TestName = "Is_FloatIsFloat" },
        new("3.14m is decimal", true) { TestName = "Is_DecimalIsDecimal" },
        new("true is bool", true) { TestName = "Is_BoolIsBool" },
        new("'a' is char", true) { TestName = "Is_CharIsChar" },
        new("\"hello\" is string", true) { TestName = "Is_StringIsString" },
        new("42 is long", false) { TestName = "Is_IntIsLong_False" },
        new("42 is double", false) { TestName = "Is_IntIsDouble_False" },
        new("42 is string", false) { TestName = "Is_IntIsString_False" },
        new("\"hello\" is int", false) { TestName = "Is_StringIsInt_False" },
        new("true is int", false) { TestName = "Is_BoolIsInt_False" },
    ];

    /// <summary>
    /// Is-type expressions with variable binding, tested via parity.
    /// Signature: (object? varValue, string expr, bool expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> IsVariableCases() =>
    [
        new(42, "x is int", true) { TestName = "Is_Variable_Int" },
        new("hello", "x is string", true) { TestName = "Is_Variable_String" },
    ];

    /// <summary>
    /// Is-object expressions with variable binding, tested via parity.
    /// Signature: (object? varValue, string expr, bool expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> IsObjectCases() =>
    [
        // ECMA-334 S12.12.12: "The result is true if ... the reference conversion succeeds"
        new(42, "x is object", true) { TestName = "Is_IntIsObject" },
        new("hello", "x is object", true) { TestName = "Is_StringIsObject" },
        new(null, "x is object", false) { TestName = "Is_NullIsObject" },
    ];

    /// <summary>
    /// Null pattern check expressions with variable binding, tested via parity.
    /// Signature: (object? varValue, string expr, bool expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> NullCheckCases() =>
    [
        // ECMA-334 S11.2.1: "A null pattern matches null values"
        new(null, "x is null", true) { TestName = "Is_NullVariable_IsNull" },
        new(null, "x is not null", false) { TestName = "Is_NullVariable_IsNotNull" },
        new("hello", "x is not null", true) { TestName = "Is_StringVariable_IsNotNull" },
    ];

    /// <summary>
    /// Is-not type pattern expressions with variable binding, tested via parity.
    /// Signature: (object? varValue, string expr, bool expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> IsNotTypeParityCases() =>
    [
        // ECMA-334 S11.2.3 -- Is Not Type Pattern (C# 9+)
        new(null, "x is not object", true) { TestName = "IsNot_NullIsNotObject_True" },
        new(null, "x is not string", true) { TestName = "IsNot_NullIsNotString_True" },
    ];

    /// <summary>
    /// Is-in-condition expressions with variable binding, tested via parity.
    /// Signature: (object varValue, string expr, object expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> IsInConditionCases() =>
    [
        // ECMA-334 S12.12.12 -- Is in Conditions and Combined with Logical Operators
        new(42, "x is int ? \"yes\" : \"no\"", "yes") { TestName = "Is_InConditional" },
        new("hello", "x is int ? \"yes\" : \"no\"", "no") { TestName = "Is_InConditional_False" },
        new(42, "(x is int) == true", true) { TestName = "Is_Precedence_WithEquality" },
        new("hello", "!(x is int)", true) { TestName = "Is_Precedence_WithNegation" },
    ];

    /// <summary>
    /// Is-with-logical-operators expressions with variable binding, tested via parity.
    /// Signature: (object varValue, string expr, bool expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> IsWithLogicalCases() =>
    [
        new("hello", "x is int || x is string", true) { TestName = "Is_WithLogicalOr" },
    ];

    /// <summary>
    /// As operator expressions with variable binding, tested via parity.
    /// Signature: (object? varValue, string expr, object? expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> AsOperatorCases() =>
    [
        // ECMA-334 S12.12.13: "The as operator is used to explicitly convert a value to a given reference type ...
        // using a reference conversion or a boxing conversion. ... the result of the operator is null."
        new("hello", "x as string", "hello") { TestName = "As_StringToString" },
        new(null, "x as string", null) { TestName = "As_NullToString" },
        new("hello", "(x as string) is not null ? \"found\" : \"not found\"", "found") { TestName = "As_InConditional" },
        new("hello", "(x as string) ?? \"default\"", "hello") { TestName = "As_ChainedWithNullCoalesce_WhenNotNull" },
    ];

    /// <summary>
    /// Is type pattern in conditional with variable binding, tested via parity.
    /// Signature: (object varValue, string expr, object expected) -- variable name "x"
    /// </summary>
    public static IEnumerable<TestCaseData> TypePatternInConditionalCases() =>
    [
        // ECMA-334 S11.2.2 -- Type Pattern with Variable Binding
        new("hello", "x is string s ? s.ToUpper() : \"not a string\"", "HELLO") { TestName = "Is_TypePattern_InConditional" },
    ];
}

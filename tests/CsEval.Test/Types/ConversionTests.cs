using CsEval.TestData.Data;

namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 sections 10.2 and 10.3 -- Implicit and Explicit conversion compliance tests.
/// Validates that CsEval's conversion behavior matches Roslyn and the ECMA-334 7th edition spec exactly.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class ConversionTests(CompilationMode mode)
{
    #region ECMA-334 §10.2.3 -- Implicit Numeric Conversions

    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ImplicitNumericCases))]
    public async Task ImplicitNumericConversions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.3 -- Guard: No Implicit Conversion TO char

    // ECMA-334 §10.2.3: "There are no predefined implicit conversions to the char type,
    // so values of the other integral types do not automatically convert to the char type."
    // Also ECMA-334 §8.3.6: "even though the byte and ushort types have ranges of values
    // that are fully representable using the char type, implicit conversions from
    // sbyte, byte, or ushort to char do not exist."

    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.GuardNoImplicitToCharCases))]
    public async Task ImplicitConversionToChar_ShouldFail(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr),
            $"Should not allow implicit conversion to char: {expr}");

        // Roslyn also rejects these
        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync(expr),
            Throws.Exception,
            $"Roslyn should also reject: {expr}");
    }

    #endregion

    #region ECMA-334 §10.3.2 -- Explicit Numeric Conversions (Cast)

    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ExplicitNumericCases))]
    public async Task ExplicitNumericConversions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.6 / §10.6.1 -- Implicit Nullable Conversions

    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ImplicitNullableCases))]
    public async Task ImplicitNullableConversions(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.11 -- Implicit Constant Expression Conversions

    // ECMA-334 §10.2.11: "A constant_expression of type int can be converted to type
    // sbyte, byte, short, ushort, uint, or ulong, provided the value of the
    // constant_expression is within the range of the destination type."

    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ImplicitConstantExprCases))]
    public async Task ImplicitConstantExpressionConversions(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Out-of-range constant expression conversions should fail
    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ConstantExprOutOfRangeCases))]
    public async Task ImplicitConstantExpressionConversions_OutOfRange_ShouldFail(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr),
            $"Should reject out-of-range constant expression: {expr}");

        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync(expr),
            Throws.Exception,
            $"Roslyn should also reject: {expr}");
    }

    #endregion

    #region ECMA-334 §10.3.2 -- Explicit Overflow Narrowing (unchecked)

    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ExplicitNarrowingOverflowCases))]
    public async Task ExplicitNarrowingOverflow(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region CanImplicitlyConvert API -- Direct Verification

    // Verify the CanImplicitlyConvert API returns correct results for key type pairs
    [Test]
    public void CanImplicitlyConvert_ByteToChar_ReturnsFalse()
    {
        // ECMA-334 §10.2.3: No implicit conversion from byte to char
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(byte), typeof(char)),
            Is.False,
            "byte -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_UShortToChar_ReturnsFalse()
    {
        // ECMA-334 §10.2.3: No implicit conversion from ushort to char
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(ushort), typeof(char)),
            Is.False,
            "ushort -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToUShort_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> ushort is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(ushort)),
            Is.True,
            "char -> ushort must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToInt_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: char -> int is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(int)),
            Is.True,
            "char -> int must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToLong_ReturnsTrue()
    {
        // ECMA-334 §10.2.3: int -> long is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long)),
            Is.True,
            "int -> long must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableInt_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: T -> T? is an implicit nullable conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(int?)),
            Is.True,
            "int -> int? must be an implicit conversion per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableLong_ReturnsTrue()
    {
        // ECMA-334 §10.6.1: S -> T? where S -> T is implicit numeric
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long?)),
            Is.True,
            "int -> long? must be an implicit conversion (lifted from int -> long) per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_FloatToDouble_ReturnsTrue()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(float), typeof(double)),
            Is.True,
            "float -> double must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_DoubleToFloat_ReturnsFalse()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(double), typeof(float)),
            Is.False,
            "double -> float is NOT an implicit conversion (requires explicit cast)");
    }

    [Test]
    public void CanImplicitlyConvert_LongToInt_ReturnsFalse()
    {
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(long), typeof(int)),
            Is.False,
            "long -> int is NOT an implicit conversion (requires explicit cast)");
    }

    #endregion

    #region Compound Assignment Type Preservation

    // ECMA-334 §12.21.4: For compound assignment x op= y, the result of x op y is
    // implicitly converted back to the type of x. The declared type is preserved.
    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.CompoundAssignmentCases))]
    public async Task CompoundAssignment_TypePreservation(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §10.2.3 -- Implicit Conversion Negative Tests (Forbidden Paths)

    // ECMA-334 §10.2.3: float -> decimal has NO implicit conversion.
    // ECMA-334 §12.4.7.3 Rule 1: "a binding-time error occurs if the other operand is of type float or double."
    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ForbiddenImplicitCases))]
    public async Task ImplicitConversion_ForbiddenPaths_ShouldFail(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr),
            $"Should not allow implicit conversion: {expr}");

        // Roslyn also rejects these
        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync(expr),
            Throws.Exception,
            $"Roslyn should also reject: {expr}");
    }

    #endregion

    #region ECMA-334 §10.3.2 -- Explicit Truncation/Narrowing Edge Cases

    // Additional explicit cast truncation and float-to-int truncation tests.
    // Note: Roslyn scripting uses checked context by default, so overflow casts like
    // (byte)256 are compile errors there. CsEval defaults to unchecked, so wrapping occurs.
    // Overflow wrapping casts use unchecked() for Roslyn parity (see §10.3.2 overflow region).
    // Float-to-int truncation works the same in both checked and unchecked contexts.
    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ExplicitTruncationCases))]
    public async Task ExplicitTruncation_EdgeCases(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Overflow narrowing casts: CsEval uses unchecked context by default.
    // These verify wrapping behavior matches unchecked C# semantics.
    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ExplicitTruncationOverflowCases))]
    public async Task ExplicitTruncation_OverflowWrapping(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 §12.18 -- Conditional Operator Type Unification

    [TestCaseSource(typeof(ConversionData), nameof(ConversionData.ConditionalTypeCases))]
    public async Task ConditionalOperator_TypeUnification(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Verify conditional with string+null returns correct type at runtime
    [Test]
    public async Task ConditionalOperator_StringAndNull_TypeIsString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("true ? \"hello\" : null");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("true ? \"hello\" : null");

        Assert.That(result, Is.EqualTo("hello"), "Value should be 'hello'");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(string)), "Type should be string");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    #endregion

    #region ECMA-334 Type System Compliance Verification

    // Criterion 1: "(byte)5 + (short)3 returns int" -- binary numeric promotion
    [Test]
    public async Task S12_4_7_BytePlusShort_ReturnsInt()
    {
        await TestHelpers.RunCSharpParityTestAsync("(byte)5 + (short)3", 8, mode);
    }

    // Criterion 2: "decimal d = 5.0f fails at evaluation time" -- no implicit float-to-decimal
    [Test]
    public async Task S10_2_FloatToDecimal_Fails()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate("{ float f = 5.0f; decimal d = f; return d; }"),
            "Implicit float -> decimal should be rejected");

        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync("{ float f = 5.0f; decimal d = f; return d; }"),
            Throws.Exception,
            "Roslyn should also reject float -> decimal");
    }

    // Criterion 3: "(byte)256 returns 0" and "(int)3.9 returns 3" -- explicit cast truncation
    // Note: (byte)256 requires unchecked context for Roslyn parity since Roslyn scripting
    // defaults to checked context. CsEval defaults to unchecked, matching normal C# behavior.
    [Test]
    public async Task S10_3_ExplicitCastTruncation()
    {
        await TestHelpers.RunCSharpParityTestAsync("unchecked((byte)256)", (byte)0, mode);
        await TestHelpers.RunCSharpParityTestAsync("(int)3.9", 3, mode);
    }

    // Criterion 4: "5U + 3 returns uint" -- constant expression conversion with uint
    [Test]
    public async Task S10_2_11_UIntPlusIntLiteral_ReturnsUInt()
    {
        await TestHelpers.RunCSharpParityTestAsync("5U + 3", 8U, mode);
    }

    // Criterion 5: "short x = 5; x += 3; x.GetType().FullName returns System.Int16" -- compound assignment type preservation
    [Test]
    public async Task S12_18_CompoundAssign_PreservesShortType()
    {
        await TestHelpers.RunCSharpParityTestAsync("{ short x = 5; x += 3; return x; }", (short)8, mode);
    }

    #endregion
}

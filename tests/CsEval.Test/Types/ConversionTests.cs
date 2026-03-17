namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 sections 10.2 and 10.3 -- Implicit and Explicit conversion compliance tests.
/// Validates that CsEval's conversion behavior matches Roslyn and the ECMA-334 7th edition spec exactly.
///
/// Engine-only tests retained here test internal TypeHelpers API directly,
/// Type object comparisons, and error assertions that cannot be expressed as .csx parity files.
/// Standard conversion expressions are in TestData/Conversion/*.csx.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class ConversionTests(CompilationMode mode)
{
    #region CanImplicitlyConvert API -- Direct Verification

    // Engine-only: TypeHelpers API tests -- verify internal API returns correct results for key type pairs

    [Test]
    public void CanImplicitlyConvert_ByteToChar_ReturnsFalse()
    {
        // Engine-only: TypeHelpers API test
        // ECMA-334 §10.2.3: No implicit conversion from byte to char
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(byte), typeof(char)),
            Is.False,
            "byte -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_UShortToChar_ReturnsFalse()
    {
        // Engine-only: TypeHelpers API test
        // ECMA-334 §10.2.3: No implicit conversion from ushort to char
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(ushort), typeof(char)),
            Is.False,
            "ushort -> char must NOT be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToUShort_ReturnsTrue()
    {
        // Engine-only: TypeHelpers API test
        // ECMA-334 §10.2.3: char -> ushort is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(ushort)),
            Is.True,
            "char -> ushort must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_CharToInt_ReturnsTrue()
    {
        // Engine-only: TypeHelpers API test
        // ECMA-334 §10.2.3: char -> int is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(char), typeof(int)),
            Is.True,
            "char -> int must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToLong_ReturnsTrue()
    {
        // Engine-only: TypeHelpers API test
        // ECMA-334 §10.2.3: int -> long is an implicit conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long)),
            Is.True,
            "int -> long must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableInt_ReturnsTrue()
    {
        // Engine-only: TypeHelpers API test
        // ECMA-334 §10.6.1: T -> T? is an implicit nullable conversion
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(int?)),
            Is.True,
            "int -> int? must be an implicit conversion per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_IntToNullableLong_ReturnsTrue()
    {
        // Engine-only: TypeHelpers API test
        // ECMA-334 §10.6.1: S -> T? where S -> T is implicit numeric
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(int), typeof(long?)),
            Is.True,
            "int -> long? must be an implicit conversion (lifted from int -> long) per ECMA-334 §10.6.1");
    }

    [Test]
    public void CanImplicitlyConvert_FloatToDouble_ReturnsTrue()
    {
        // Engine-only: TypeHelpers API test
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(float), typeof(double)),
            Is.True,
            "float -> double must be an implicit conversion per ECMA-334 §10.2.3");
    }

    [Test]
    public void CanImplicitlyConvert_DoubleToFloat_ReturnsFalse()
    {
        // Engine-only: TypeHelpers API test
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(double), typeof(float)),
            Is.False,
            "double -> float is NOT an implicit conversion (requires explicit cast)");
    }

    [Test]
    public void CanImplicitlyConvert_LongToInt_ReturnsFalse()
    {
        // Engine-only: TypeHelpers API test
        Assert.That(
            CsEval.Runtime.TypeHelpers.CanImplicitlyConvert(typeof(long), typeof(int)),
            Is.False,
            "long -> int is NOT an implicit conversion (requires explicit cast)");
    }

    #endregion

    #region ECMA-334 §12.18 -- Conditional Operator Type Unification

    [Test]
    public async Task ConditionalOperator_StringAndNull_TypeIsString()
    {
        // Engine-only: Type object comparison (verifies result type is typeof(string))
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate("true ? \"hello\" : null");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("true ? \"hello\" : null");

        Assert.That(result, Is.EqualTo("hello"), "Value should be 'hello'");
        Assert.That(result?.GetType(), Is.EqualTo(typeof(string)), "Type should be string");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), "Type must match Roslyn");
    }

    #endregion

    #region ECMA-334 Type System Compliance Verification

    // Engine-only: error test -- implicit float-to-decimal must be rejected
    [Test]
    public async Task S10_2_FloatToDecimal_Fails()
    {
        // Engine-only: error assertion (both CsEval and Roslyn must throw)
        var engine = TestEngineFactory.Create(mode);
        Assert.Catch<Exception>(() => engine.Evaluate("{ float f = 5.0f; decimal d = f; return d; }"),
            "Implicit float -> decimal should be rejected");

        await Assert.ThatAsync(
            async () => await TestHelpers.EvaluateCSharpAsync("{ float f = 5.0f; decimal d = f; return d; }"),
            Throws.Exception,
            "Roslyn should also reject float -> decimal");
    }

    #endregion
}

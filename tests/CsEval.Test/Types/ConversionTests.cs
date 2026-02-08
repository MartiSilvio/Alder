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

    #region ECMA-334 §12.18 -- Conditional Operator Type Unification

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

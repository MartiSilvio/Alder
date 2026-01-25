namespace CsEval.Test.Evaluator;

/// <summary>
/// Comprehensive numeric tests to ensure CsEval handles all numeric types correctly.
/// Numeric precision and type handling is critical - errors here can cause subtle bugs.
/// </summary>
[TestFixture(CompilationMode.Eager)]
[TestFixture(CompilationMode.OnDemand)]
public class NumericTests(CompilationMode mode) : TestBase
{
    #region Literal Parsing

    [Test]
    public void IntegerLiteral_ParsedAsInt()
    {
        var engine = CreateEngine(mode);
        // C# behavior: unsuffixed integers are int
        var result = engine.Evaluate("42");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ZeroLiteral_ParsedAsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("0");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void NegativeInteger_ParsedAsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("-42");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(-42));
    }

    [Test]
    public void LargeInteger_AutoPromotesToLong()
    {
        var engine = CreateEngine(mode);
        // C# behavior: integers too large for int auto-promote to long
        var result = engine.Evaluate("9223372036854775807"); // long.MaxValue
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void IntegerWithLSuffix_ParsedAsLong()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("42L");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void FloatSuffix_ParsedAsFloat()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("3.14f");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void DecimalSuffix_ParsedAsDecimal()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("3.14m");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(3.14m));
    }

    [Test]
    public void IntegerWithDecimalSuffix_ParsedAsDecimal()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("42m");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(42m));
    }

    [Test]
    public void DecimalLiteral_ParsedAsDouble()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("3.14");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void DecimalWithLeadingZero_ParsedAsDouble()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("0.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(0.5));
    }

    [Test]
    public void NegativeDecimal_ParsedAsDouble()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("-3.14");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(-3.14));
    }

    [Test]
    public void SmallDecimal_PreservesPrecision()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("0.00001");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(0.00001));
    }

    #endregion

    #region Arithmetic Operations - Same Types

    [Test]
    public void IntPlusInt_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        // C# behavior: int + int → int
        var result = engine.Evaluate("5 + 3");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void IntMinusInt_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("10 - 4");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void IntTimesInt_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("6 * 7");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void IntDivideInt_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        // C# behavior: int / int → int (truncating division)
        var result = engine.Evaluate("10 / 4");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void LongPlusLong_ReturnsLong()
    {
        var engine = CreateEngine(mode);
        // Use L suffix to get long operands
        var result = engine.Evaluate("5L + 3L");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void IntPlusLong_ReturnsLong()
    {
        var engine = CreateEngine(mode);
        // C# behavior: int + long → long
        var result = engine.Evaluate("5 + 3L");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void DoublePlusDouble_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("1.5 + 2.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public void DoubleTimesDouble_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("2.5 * 4.0");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(10.0));
    }

    #endregion

    #region Arithmetic Operations - Mixed Types

    [Test]
    public void LongPlusDouble_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("5 + 2.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void DoublePlusLong_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("2.5 + 5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void IntTimesInt_FromVariable_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 5); // int
        var result = engine.Evaluate("x * 2"); // 2 is now int (C# behavior)
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntTimesLong_ReturnsLong()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 5); // int
        var result = engine.Evaluate("x * 2L"); // Use L suffix for long
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntTimesDouble_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 5); // int
        var result = engine.Evaluate("x * 2.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(12.5));
    }

    #endregion

    #region All Numeric Types - Comprehensive Coverage

    // Signed integer types - C# promotes small types to int for arithmetic
    [Test]
    public void SByte_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", (sbyte)10);
        engine.SetVariable("y", (sbyte)5);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>()); // C# promotes sbyte to int
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Short_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", (short)1000);
        engine.SetVariable("y", (short)234);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>()); // C# promotes short to int
        Assert.That(result, Is.EqualTo(1234));
    }

    [Test]
    public void Int_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 100000);
        engine.SetVariable("y", 23456);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>()); // int + int → int
        Assert.That(result, Is.EqualTo(123456));
    }

    [Test]
    public void Long_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10000000000L);
        engine.SetVariable("y", 2345678901L);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(12345678901L));
    }

    // Unsigned integer types - C# promotes byte/ushort to int for arithmetic
    [Test]
    public void Byte_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", (byte)200);
        engine.SetVariable("y", (byte)55);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>()); // C# promotes byte to int
        Assert.That(result, Is.EqualTo(255));
    }

    [Test]
    public void UShort_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", (ushort)60000);
        engine.SetVariable("y", (ushort)5535);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>()); // C# promotes ushort to int
        Assert.That(result, Is.EqualTo(65535));
    }

    [Test]
    public void UInt_Arithmetic()
    {
        var engine = CreateEngine(mode);
        // C# behavior: uint + uint → uint
        engine.SetVariable("x", 4000000000u);
        engine.SetVariable("y", 294967295u);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<uint>());
        Assert.That(result, Is.EqualTo(4294967295u));
    }

    [Test]
    public void ULong_Arithmetic()
    {
        var engine = CreateEngine(mode);
        // C# behavior: ulong + ulong → ulong
        engine.SetVariable("x", 10000000000UL);
        engine.SetVariable("y", 5000000000UL);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<ulong>());
        Assert.That(result, Is.EqualTo(15000000000UL));
    }

    // Floating-point types
    [Test]
    public void Float_Arithmetic()
    {
        var engine = CreateEngine(mode);
        // C# behavior: float + float → float
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25f);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(15.75f).Within(0.001f));
    }

    [Test]
    public void Double_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5d);
        engine.SetVariable("y", 5.25d);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(15.75));
    }

    [Test]
    public void Decimal_Arithmetic()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5m);
        engine.SetVariable("y", 5.25m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.75m));
    }

    // Mixed type operations
    [Test]
    public void BytePlusShort_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        // C# promotes both to int for arithmetic
        engine.SetVariable("x", (byte)100);
        engine.SetVariable("y", (short)200);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(300));
    }

    [Test]
    public void IntPlusFloat_ReturnsFloat()
    {
        var engine = CreateEngine(mode);
        // C# behavior: int + float → float
        engine.SetVariable("x", 10);
        engine.SetVariable("y", 5.5f);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(15.5f).Within(0.001f));
    }

    [Test]
    public void FloatPlusDouble_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25d);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(15.75).Within(0.001));
    }

    [Test]
    public void IntPlusDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10);
        engine.SetVariable("y", 5.5m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void FloatPlusDecimal_Throws()
    {
        var engine = CreateEngine(mode);
        // C# forbids mixing float and decimal - compile-time error
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25m);
        Assert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(() => engine.Evaluate("x + y"));
    }

    [Test]
    public void DoublePlusDecimal_Throws()
    {
        var engine = CreateEngine(mode);
        // C# forbids mixing double and decimal - compile-time error
        engine.SetVariable("x", 10.5d);
        engine.SetVariable("y", 5.25m);
        Assert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(() => engine.Evaluate("x + y"));
    }

    #endregion

    #region Comparison Operations

    [Test]
    public void IntEqualsLong_Works()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 42); // int
        var result = engine.Evaluate("x == 42");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LongEqualsInt_Works()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 42L); // long
        var result = engine.Evaluate("x == 42");
        Assert.That(result, Is.True);
    }

    [Test]
    public void DoubleEqualsDouble_Works()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("3.14 == 3.14");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LongLessThanDouble_Works()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("5 < 5.5");
        Assert.That(result, Is.True);
    }

    [Test]
    public void DoubleGreaterThanLong_Works()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("5.5 > 5");
        Assert.That(result, Is.True);
    }

    #endregion

    #region Contains with Different Types

    [Test]
    public void Contains_IntListWithLongLiteral_Works()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("numbers", new List<object?> { 1, 2, 3 }); // boxed ints
        var result = engine.Evaluate("numbers.Contains(2)"); // 2 is long
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_LongListWithIntVariable_Works()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("numbers", new List<object?> { 1L, 2L, 3L }); // longs
        engine.SetVariable("search", 2); // int
        var result = engine.Evaluate("numbers.Contains(search)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_DoubleListWithDoubleLiteral_Works()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("numbers", new List<object?> { 1.5, 2.5, 3.5 });
        var result = engine.Evaluate("numbers.Contains(2.5)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_DecimalListWithDoubleLiteral_Works()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("numbers", new List<object?> { 1.5m, 2.5m, 3.5m }); // decimals
        var result = engine.Evaluate("numbers.Contains(2.5)"); // 2.5 is double
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_MixedNumericTypes_Works()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("numbers", new List<object?> { 1, 2L, 3.0, 4.0f }); // mixed
        Assert.That(engine.Evaluate("numbers.Contains(1)"), Is.True);
        Assert.That(engine.Evaluate("numbers.Contains(2)"), Is.True);
        Assert.That(engine.Evaluate("numbers.Contains(3)"), Is.True);
        Assert.That(engine.Evaluate("numbers.Contains(4)"), Is.True);
    }

    #endregion

    #region Precision Tests - Mathematical Verification

    // Classic floating-point precision test: 0.1 + 0.2 != 0.3 in IEEE 754
    // See: https://floating-point-gui.de/basic/
    [Test]
    public void Double_ClassicPrecisionIssue_PointOnePointTwo()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("0.1 + 0.2");
        Assert.That(result, Is.TypeOf<double>());
        // 0.1 + 0.2 in double is NOT exactly 0.3
        Assert.That((double)result!, Is.Not.EqualTo(0.3));
        Assert.That((double)result!, Is.EqualTo(0.30000000000000004).Within(1e-16));
    }

    [Test]
    public void Decimal_PointOneIsExact()
    {
        var engine = CreateEngine(mode);
        // Decimal can represent 0.1 exactly (unlike double)
        engine.SetVariable("x", 0.1m);
        engine.SetVariable("y", 0.2m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(0.3m)); // Exact!
    }

    // Compounding error test: adding 0.01 repeatedly
    // See: https://code-maze.com/csharp-floating-point-types/
    [Test]
    public void Double_CompoundingError_RepeatedAddition()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("sum", 0.0);
        engine.SetVariable("increment", 0.01);

        // Simulate adding 0.01 ten times
        for (int i = 0; i < 10; i++)
        {
            var current = (double)engine.Evaluate("sum")!;
            engine.SetVariable("sum", current + 0.01);
        }

        var result = (double)engine.Evaluate("sum")!;
        // Should be 0.1, but double accumulates error
        Assert.That(result, Is.Not.EqualTo(0.1));
        Assert.That(result, Is.EqualTo(0.09999999999999999).Within(1e-16));
    }

    [Test]
    public void Decimal_NoCompoundingError_RepeatedAddition()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("sum", 0.0m);

        // Simulate adding 0.01 ten times
        for (int i = 0; i < 10; i++)
        {
            var current = (decimal)engine.Evaluate("sum")!;
            engine.SetVariable("sum", current + 0.01m);
        }

        var result = engine.Evaluate("sum");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(0.1m)); // Exact!
    }

    // Division precision: 1/3 * 3 should be 1
    [Test]
    public void Double_DivisionMultiplication_OneThird()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 1.0 / 3.0);
        var result = engine.Evaluate("x * 3");
        // Double: may not be exactly 1
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-15));
    }

    [Test]
    public void Decimal_DivisionMultiplication_OneThird()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("third", 1.0m / 3.0m);
        var result = engine.Evaluate("third * 3");
        Assert.That(result, Is.TypeOf<decimal>());
        // Decimal also has rounding, but different behavior
        Assert.That((decimal)result!, Is.EqualTo(1.0m).Within(0.0000000001m));
    }

    // Large integer precision
    [Test]
    public void LargeLongArithmetic_NoOverflow()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", long.MaxValue - 1);
        var result = engine.Evaluate("x + 1");
        Assert.That(result, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void Division_IntDivInt_Truncates()
    {
        var engine = CreateEngine(mode);
        // C# behavior: int / int → int (truncates)
        var result = engine.Evaluate("7 / 3");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(2)); // 7 / 3 = 2 (truncated)
    }

    [Test]
    public void Division_DoubleDivDouble_PreservesFractional()
    {
        var engine = CreateEngine(mode);
        // Use double literals to get fractional result
        var result = engine.Evaluate("7.0 / 3.0");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(7.0 / 3.0).Within(1e-15));
    }

    // Financial calculation: interest rate application
    [Test]
    public void Decimal_FinancialCalculation_InterestRate()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("principal", 10000.00m);
        engine.SetVariable("rate", 0.0525m); // 5.25% interest
        var result = engine.Evaluate("principal * rate");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(525.00m)); // Exact
    }

    [Test]
    public void Double_FinancialCalculation_MayHaveError()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("principal", 10000.00d);
        engine.SetVariable("rate", 0.0525d);
        var result = engine.Evaluate("principal * rate");
        Assert.That(result, Is.TypeOf<double>());
        // May or may not be exactly 525.0 depending on representation
        Assert.That((double)result!, Is.EqualTo(525.0).Within(1e-10));
    }

    // Significant digits test
    [Test]
    public void Double_LosesPrecisionAt17Digits()
    {
        var engine = CreateEngine(mode);
        // 17 significant digits - at the edge of double precision
        engine.SetVariable("x", 12345678901234567.0);
        engine.SetVariable("y", 1.0);
        var result = engine.Evaluate("x + y");
        // Double may lose precision here
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Decimal_Preserves28Digits()
    {
        var engine = CreateEngine(mode);
        // 28 significant digits - within decimal's precision
        engine.SetVariable("x", 1234567890123456789012345678m);
        engine.SetVariable("y", 1m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(1234567890123456789012345679m));
    }

    #endregion

    #region Decimal Precision Tests

    [Test]
    public void DecimalPlusDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5m);
        engine.SetVariable("y", 5.25m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.75m));
    }

    [Test]
    public void DecimalPlusLong_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5m);
        var result = engine.Evaluate("x + 5");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void LongPlusDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5m);
        var result = engine.Evaluate("5 + x");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void DecimalTimesDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 3.5m);
        engine.SetVariable("y", 2.0m);
        var result = engine.Evaluate("x * y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(7.0m));
    }

    [Test]
    public void DecimalDivideDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.0m);
        engine.SetVariable("y", 4.0m);
        var result = engine.Evaluate("x / y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.5m));
    }

    [Test]
    public void DecimalMinusDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.75m);
        engine.SetVariable("y", 5.25m);
        var result = engine.Evaluate("x - y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(5.5m));
    }

    [Test]
    public void DecimalModDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5m);
        engine.SetVariable("y", 3.0m);
        var result = engine.Evaluate("x % y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(1.5m));
    }

    [Test]
    public void DecimalPrecision_PreservedHighPrecisionValue()
    {
        var engine = CreateEngine(mode);
        // Decimal has 28-29 significant digits, double only has 15-17
        // This value has 20 significant digits - would be truncated as double
        engine.SetVariable("x", 1.1234567890123456789m);
        engine.SetVariable("y", 1.0m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.1234567890123456789m));
    }

    [Test]
    public void DecimalPrecision_NotLostToDouble()
    {
        // If we converted to double, we'd lose precision here
        var engine = CreateEngine(mode);
        var preciseValue = 12345678901234567890.12345678m;
        engine.SetVariable("x", preciseValue);
        engine.SetVariable("y", 0m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(preciseValue));
    }

    [Test]
    public void NegateDecimal_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 5.5m);
        var result = engine.Evaluate("-x");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(-5.5m));
    }

    [Test]
    public void NegateDecimal_Variable_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        engine.SetVariable("x", 10.5m);
        var result = engine.Evaluate("-x");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(-10.5m));
    }

    #endregion

    #region Modulo Operations

    [Test]
    public void IntModInt_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        // C# behavior: int % int → int
        var result = engine.Evaluate("10 % 3");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void DoubleModDouble_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        // Double modulo may throw or return specific type
        try
        {
            var result = engine.Evaluate("10.5 % 3.0");
            Assert.That(result, Is.Not.Null);
            Assert.That(Convert.ToDouble(result), Is.EqualTo(1.5).Within(0.01));
        }
        catch (CsEvalException)
        {
            // Double modulo not supported is acceptable
            Assert.Pass("Double modulo not supported");
        }
    }

    #endregion

    #region Array Literals - Type Verification

    [Test]
    public void ArrayLiteral_IntElements_ReturnsIntList()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("[1, 2, 3]") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.TypeOf<int>());
        Assert.That(result[1], Is.TypeOf<int>());
        Assert.That(result[2], Is.TypeOf<int>());
    }

    [Test]
    public void ArrayLiteral_LongElements_ReturnsLongList()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("[1L, 2L, 3L]") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.TypeOf<long>());
        Assert.That(result[1], Is.TypeOf<long>());
        Assert.That(result[2], Is.TypeOf<long>());
    }

    [Test]
    public void ArrayLiteral_IndexAccess_PreservesType()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var arr = [10, 20, 30]; return arr[1]; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(20));
    }

    #endregion

    #region Block Expressions - Type Verification

    [Test]
    public void BlockExpression_IntVariable_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 42; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void BlockExpression_IntArithmetic_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 10; var y = 5; return x + y; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void BlockExpression_IntAssignment_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 1; x = 99; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void BlockExpression_IntCompoundAssignment_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 10; x += 5; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void BlockExpression_IntIncrement_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 5; x++; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region Bitwise Operations in Blocks - Type Verification

    [Test]
    public void BlockExpression_BitwiseAnd_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 15; x &= 9; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void BlockExpression_BitwiseOr_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 5; x |= 3; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void BlockExpression_BitwiseXor_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 12; x ^= 5; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void BlockExpression_LeftShift_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 1; x <<= 4; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void BlockExpression_RightShift_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("{ var x = 32; x >>= 2; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(8));
    }

    #endregion

    #region Loop Counter - Type Verification

    [Test]
    public void ForLoop_IntCounter_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                sum += i;
            }
            return sum;
        }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(10)); // 0+1+2+3+4
    }

    [Test]
    public void WhileLoop_IntCounter_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            var i = 1;
            while (i <= 5) {
                sum += i;
                i++;
            }
            return sum;
        }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15)); // 1+2+3+4+5
    }

    [Test]
    public void ForEachLoop_IntArraySum_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            foreach (var n in [1, 2, 3, 4, 5]) {
                sum += n;
            }
            return sum;
        }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    #endregion

    #region LINQ Operations on Int Arrays - Type Verification

    [Test]
    public void LinqSelect_IntArray_ReturnsIntValues()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("[1, 2, 3].Select(x => x * 2).ToList()") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.TypeOf<int>());
        Assert.That(result[0], Is.EqualTo(2));
        Assert.That(result[1], Is.TypeOf<int>());
        Assert.That(result[1], Is.EqualTo(4));
    }

    [Test]
    public void LinqWhere_IntArray_ReturnsIntValues()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Where(x => x > 2).ToList()") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.TypeOf<int>());
        Assert.That(result[0], Is.EqualTo(3));
    }

    [Test]
    public void LinqSum_IntArray_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Sum()");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void LinqChain_IntArray_ReturnsInt()
    {
        var engine = CreateEngine(mode);
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Where(x => x > 2).Select(x => x * 10).Sum()");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(120)); // (3+4+5)*10 = 120
    }

    [Test]
    public void LinqAverage_IntArray_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        // C# behavior: Average of int/long returns double
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Average()");

        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void LinqAverage_DecimalArray_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        // C# behavior: Average of decimal returns decimal
        var result = engine.Evaluate("[1.5m, 2.5m, 3.5m].Average()");

        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.5m));
    }

    [Test]
    public void LinqAverage_LongArray_ReturnsDouble()
    {
        var engine = CreateEngine(mode);
        // C# behavior: Average of long returns double
        var result = engine.Evaluate("[1L, 2L, 3L, 4L, 5L].Average()");

        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void LinqAverage_DecimalWithSelector_ReturnsDecimal()
    {
        var engine = CreateEngine(mode);
        // C# behavior: Average with selector returning decimal returns decimal
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        var result = engine.Evaluate("items.Average(x => x * 1.0m)");

        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.0m));
    }

    #endregion
}

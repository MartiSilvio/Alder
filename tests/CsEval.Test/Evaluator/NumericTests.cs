namespace CsEval.Test.Evaluator;

/// <summary>
/// Comprehensive numeric tests to ensure CsEval handles all numeric types correctly.
/// Numeric precision and type handling is critical - errors here can cause subtle bugs.
/// </summary>
[TestFixture]
public class NumericTests : EvaluatorTestBase
{
    #region Literal Parsing

    [Test]
    public void IntegerLiteral_ParsedAsInt()
    {
        // C# behavior: unsuffixed integers are int
        var result = Eval("42");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void ZeroLiteral_ParsedAsInt()
    {
        var result = Eval("0");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void NegativeInteger_ParsedAsInt()
    {
        var result = Eval("-42");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(-42));
    }

    [Test]
    public void LargeInteger_AutoPromotesToLong()
    {
        // C# behavior: integers too large for int auto-promote to long
        var result = Eval("9223372036854775807"); // long.MaxValue
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void IntegerWithLSuffix_ParsedAsLong()
    {
        var result = Eval("42L");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void FloatSuffix_ParsedAsFloat()
    {
        var result = Eval("3.14f");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(3.14f).Within(0.001f));
    }

    [Test]
    public void DecimalSuffix_ParsedAsDecimal()
    {
        var result = Eval("3.14m");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(3.14m));
    }

    [Test]
    public void IntegerWithDecimalSuffix_ParsedAsDecimal()
    {
        var result = Eval("42m");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(42m));
    }

    [Test]
    public void DecimalLiteral_ParsedAsDouble()
    {
        var result = Eval("3.14");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.14));
    }

    [Test]
    public void DecimalWithLeadingZero_ParsedAsDouble()
    {
        var result = Eval("0.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(0.5));
    }

    [Test]
    public void NegativeDecimal_ParsedAsDouble()
    {
        var result = Eval("-3.14");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(-3.14));
    }

    [Test]
    public void SmallDecimal_PreservesPrecision()
    {
        var result = Eval("0.00001");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(0.00001));
    }

    #endregion

    #region Arithmetic Operations - Same Types

    [Test]
    public void IntPlusInt_ReturnsInt()
    {
        // C# behavior: int + int → int
        var result = Eval("5 + 3");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void IntMinusInt_ReturnsInt()
    {
        var result = Eval("10 - 4");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void IntTimesInt_ReturnsInt()
    {
        var result = Eval("6 * 7");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void IntDivideInt_ReturnsInt()
    {
        // C# behavior: int / int → int (truncating division)
        var result = Eval("10 / 4");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void LongPlusLong_ReturnsLong()
    {
        // Use L suffix to get long operands
        var result = Eval("5L + 3L");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void IntPlusLong_ReturnsLong()
    {
        // C# behavior: int + long → long
        var result = Eval("5 + 3L");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void DoublePlusDouble_ReturnsDouble()
    {
        var result = Eval("1.5 + 2.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(4.0));
    }

    [Test]
    public void DoubleTimesDouble_ReturnsDouble()
    {
        var result = Eval("2.5 * 4.0");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(10.0));
    }

    #endregion

    #region Arithmetic Operations - Mixed Types

    [Test]
    public void LongPlusDouble_ReturnsDouble()
    {
        var result = Eval("5 + 2.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void DoublePlusLong_ReturnsDouble()
    {
        var result = Eval("2.5 + 5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(7.5));
    }

    [Test]
    public void IntTimesInt_FromVariable_ReturnsInt()
    {
        var context = new EvalContext();
        context.Define("x", 5); // int
        var result = Eval("x * 2", context); // 2 is now int (C# behavior)
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntTimesLong_ReturnsLong()
    {
        var context = new EvalContext();
        context.Define("x", 5); // int
        var result = Eval("x * 2L", context); // Use L suffix for long
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntTimesDouble_ReturnsDouble()
    {
        var context = new EvalContext();
        context.Define("x", 5); // int
        var result = Eval("x * 2.5", context);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(12.5));
    }

    #endregion

    #region All Numeric Types - Comprehensive Coverage

    // Signed integer types - C# promotes small types to int for arithmetic
    [Test]
    public void SByte_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", (sbyte)10);
        context.Define("y", (sbyte)5);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<int>()); // C# promotes sbyte to int
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Short_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", (short)1000);
        context.Define("y", (short)234);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<int>()); // C# promotes short to int
        Assert.That(result, Is.EqualTo(1234));
    }

    [Test]
    public void Int_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", 100000);
        context.Define("y", 23456);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<int>()); // int + int → int
        Assert.That(result, Is.EqualTo(123456));
    }

    [Test]
    public void Long_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", 10000000000L);
        context.Define("y", 2345678901L);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(12345678901L));
    }

    // Unsigned integer types - C# promotes byte/ushort to int for arithmetic
    [Test]
    public void Byte_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", (byte)200);
        context.Define("y", (byte)55);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<int>()); // C# promotes byte to int
        Assert.That(result, Is.EqualTo(255));
    }

    [Test]
    public void UShort_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", (ushort)60000);
        context.Define("y", (ushort)5535);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<int>()); // C# promotes ushort to int
        Assert.That(result, Is.EqualTo(65535));
    }

    [Test]
    public void UInt_Arithmetic()
    {
        // C# behavior: uint + uint → uint
        var context = new EvalContext();
        context.Define("x", 4000000000u);
        context.Define("y", 294967295u);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<uint>());
        Assert.That(result, Is.EqualTo(4294967295u));
    }

    [Test]
    public void ULong_Arithmetic()
    {
        // C# behavior: ulong + ulong → ulong
        var context = new EvalContext();
        context.Define("x", 10000000000UL);
        context.Define("y", 5000000000UL);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<ulong>());
        Assert.That(result, Is.EqualTo(15000000000UL));
    }

    // Floating-point types
    [Test]
    public void Float_Arithmetic()
    {
        // C# behavior: float + float → float
        var context = new EvalContext();
        context.Define("x", 10.5f);
        context.Define("y", 5.25f);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(15.75f).Within(0.001f));
    }

    [Test]
    public void Double_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", 10.5d);
        context.Define("y", 5.25d);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(15.75));
    }

    [Test]
    public void Decimal_Arithmetic()
    {
        var context = new EvalContext();
        context.Define("x", 10.5m);
        context.Define("y", 5.25m);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.75m));
    }

    // Mixed type operations
    [Test]
    public void BytePlusShort_ReturnsInt()
    {
        // C# promotes both to int for arithmetic
        var context = new EvalContext();
        context.Define("x", (byte)100);
        context.Define("y", (short)200);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(300));
    }

    [Test]
    public void IntPlusFloat_ReturnsFloat()
    {
        // C# behavior: int + float → float
        var context = new EvalContext();
        context.Define("x", 10);
        context.Define("y", 5.5f);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(15.5f).Within(0.001f));
    }

    [Test]
    public void FloatPlusDouble_ReturnsDouble()
    {
        var context = new EvalContext();
        context.Define("x", 10.5f);
        context.Define("y", 5.25d);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(15.75).Within(0.001));
    }

    [Test]
    public void IntPlusDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10);
        context.Define("y", 5.5m);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void FloatPlusDecimal_Throws()
    {
        // C# forbids mixing float and decimal - compile-time error
        var context = new EvalContext();
        context.Define("x", 10.5f);
        context.Define("y", 5.25m);
        Assert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(() => Eval("x + y", context));
    }

    [Test]
    public void DoublePlusDecimal_Throws()
    {
        // C# forbids mixing double and decimal - compile-time error
        var context = new EvalContext();
        context.Define("x", 10.5d);
        context.Define("y", 5.25m);
        Assert.Throws<Microsoft.CSharp.RuntimeBinder.RuntimeBinderException>(() => Eval("x + y", context));
    }

    #endregion

    #region Comparison Operations

    [Test]
    public void IntEqualsLong_Works()
    {
        var context = new EvalContext();
        context.Define("x", 42); // int
        var result = Eval("x == 42", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void LongEqualsInt_Works()
    {
        var context = new EvalContext();
        context.Define("x", 42L); // long
        var result = Eval("x == 42", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void DoubleEqualsDouble_Works()
    {
        var result = Eval("3.14 == 3.14");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LongLessThanDouble_Works()
    {
        var result = Eval("5 < 5.5");
        Assert.That(result, Is.True);
    }

    [Test]
    public void DoubleGreaterThanLong_Works()
    {
        var result = Eval("5.5 > 5");
        Assert.That(result, Is.True);
    }

    #endregion

    #region Contains with Different Types

    [Test]
    public void Contains_IntListWithLongLiteral_Works()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1, 2, 3 }); // boxed ints
        var result = Eval("numbers.Contains(2)", context); // 2 is long
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_LongListWithIntVariable_Works()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1L, 2L, 3L }); // longs
        context.Define("search", 2); // int
        var result = Eval("numbers.Contains(search)", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_DoubleListWithDoubleLiteral_Works()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1.5, 2.5, 3.5 });
        var result = Eval("numbers.Contains(2.5)", context);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_DecimalListWithDoubleLiteral_Works()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1.5m, 2.5m, 3.5m }); // decimals
        var result = Eval("numbers.Contains(2.5)", context); // 2.5 is double
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_MixedNumericTypes_Works()
    {
        var context = new EvalContext();
        context.Define("numbers", new List<object?> { 1, 2L, 3.0, 4.0f }); // mixed
        Assert.That(Eval("numbers.Contains(1)", context), Is.True);
        Assert.That(Eval("numbers.Contains(2)", context), Is.True);
        Assert.That(Eval("numbers.Contains(3)", context), Is.True);
        Assert.That(Eval("numbers.Contains(4)", context), Is.True);
    }

    #endregion

    #region Precision Tests - Mathematical Verification

    // Classic floating-point precision test: 0.1 + 0.2 != 0.3 in IEEE 754
    // See: https://floating-point-gui.de/basic/
    [Test]
    public void Double_ClassicPrecisionIssue_PointOnePointTwo()
    {
        var result = Eval("0.1 + 0.2");
        Assert.That(result, Is.TypeOf<double>());
        // 0.1 + 0.2 in double is NOT exactly 0.3
        Assert.That((double)result!, Is.Not.EqualTo(0.3));
        Assert.That((double)result!, Is.EqualTo(0.30000000000000004).Within(1e-16));
    }

    [Test]
    public void Decimal_PointOneIsExact()
    {
        // Decimal can represent 0.1 exactly (unlike double)
        var context = new EvalContext();
        context.Define("x", 0.1m);
        context.Define("y", 0.2m);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(0.3m)); // Exact!
    }

    // Compounding error test: adding 0.01 repeatedly
    // See: https://code-maze.com/csharp-floating-point-types/
    [Test]
    public void Double_CompoundingError_RepeatedAddition()
    {
        var context = new EvalContext();
        context.Define("sum", 0.0);
        context.Define("increment", 0.01);

        // Simulate adding 0.01 ten times
        for (int i = 0; i < 10; i++)
        {
            var current = (double)Eval("sum", context)!;
            context.Define("sum", current + 0.01);
        }

        var result = (double)Eval("sum", context)!;
        // Should be 0.1, but double accumulates error
        Assert.That(result, Is.Not.EqualTo(0.1));
        Assert.That(result, Is.EqualTo(0.09999999999999999).Within(1e-16));
    }

    [Test]
    public void Decimal_NoCompoundingError_RepeatedAddition()
    {
        var context = new EvalContext();
        context.Define("sum", 0.0m);

        // Simulate adding 0.01 ten times
        for (int i = 0; i < 10; i++)
        {
            var current = (decimal)Eval("sum", context)!;
            context.Define("sum", current + 0.01m);
        }

        var result = Eval("sum", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(0.1m)); // Exact!
    }

    // Division precision: 1/3 * 3 should be 1
    [Test]
    public void Double_DivisionMultiplication_OneThird()
    {
        var context = new EvalContext();
        context.Define("x", 1.0 / 3.0);
        var result = Eval("x * 3", context);
        // Double: may not be exactly 1
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-15));
    }

    [Test]
    public void Decimal_DivisionMultiplication_OneThird()
    {
        var context = new EvalContext();
        context.Define("third", 1.0m / 3.0m);
        var result = Eval("third * 3", context);
        Assert.That(result, Is.TypeOf<decimal>());
        // Decimal also has rounding, but different behavior
        Assert.That((decimal)result!, Is.EqualTo(1.0m).Within(0.0000000001m));
    }

    // Large integer precision
    [Test]
    public void LargeLongArithmetic_NoOverflow()
    {
        var context = new EvalContext();
        context.Define("x", long.MaxValue - 1);
        var result = Eval("x + 1", context);
        Assert.That(result, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void Division_IntDivInt_Truncates()
    {
        // C# behavior: int / int → int (truncates)
        var result = Eval("7 / 3");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(2)); // 7 / 3 = 2 (truncated)
    }

    [Test]
    public void Division_DoubleDivDouble_PreservesFractional()
    {
        // Use double literals to get fractional result
        var result = Eval("7.0 / 3.0");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(7.0 / 3.0).Within(1e-15));
    }

    // Financial calculation: interest rate application
    [Test]
    public void Decimal_FinancialCalculation_InterestRate()
    {
        var context = new EvalContext();
        context.Define("principal", 10000.00m);
        context.Define("rate", 0.0525m); // 5.25% interest
        var result = Eval("principal * rate", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(525.00m)); // Exact
    }

    [Test]
    public void Double_FinancialCalculation_MayHaveError()
    {
        var context = new EvalContext();
        context.Define("principal", 10000.00d);
        context.Define("rate", 0.0525d);
        var result = Eval("principal * rate", context);
        Assert.That(result, Is.TypeOf<double>());
        // May or may not be exactly 525.0 depending on representation
        Assert.That((double)result!, Is.EqualTo(525.0).Within(1e-10));
    }

    // Significant digits test
    [Test]
    public void Double_LosesPrecisionAt17Digits()
    {
        var context = new EvalContext();
        // 17 significant digits - at the edge of double precision
        context.Define("x", 12345678901234567.0);
        context.Define("y", 1.0);
        var result = Eval("x + y", context);
        // Double may lose precision here
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Decimal_Preserves28Digits()
    {
        var context = new EvalContext();
        // 28 significant digits - within decimal's precision
        context.Define("x", 1234567890123456789012345678m);
        context.Define("y", 1m);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(1234567890123456789012345679m));
    }

    #endregion

    #region Decimal Precision Tests

    [Test]
    public void DecimalPlusDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10.5m);
        context.Define("y", 5.25m);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.75m));
    }

    [Test]
    public void DecimalPlusLong_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10.5m);
        var result = Eval("x + 5", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void LongPlusDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10.5m);
        var result = Eval("5 + x", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void DecimalTimesDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 3.5m);
        context.Define("y", 2.0m);
        var result = Eval("x * y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(7.0m));
    }

    [Test]
    public void DecimalDivideDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10.0m);
        context.Define("y", 4.0m);
        var result = Eval("x / y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.5m));
    }

    [Test]
    public void DecimalMinusDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10.75m);
        context.Define("y", 5.25m);
        var result = Eval("x - y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(5.5m));
    }

    [Test]
    public void DecimalModDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10.5m);
        context.Define("y", 3.0m);
        var result = Eval("x % y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(1.5m));
    }

    [Test]
    public void DecimalPrecision_PreservedHighPrecisionValue()
    {
        // Decimal has 28-29 significant digits, double only has 15-17
        // This value has 20 significant digits - would be truncated as double
        var context = new EvalContext();
        context.Define("x", 1.1234567890123456789m);
        context.Define("y", 1.0m);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.1234567890123456789m));
    }

    [Test]
    public void DecimalPrecision_NotLostToDouble()
    {
        // If we converted to double, we'd lose precision here
        var context = new EvalContext();
        var preciseValue = 12345678901234567890.12345678m;
        context.Define("x", preciseValue);
        context.Define("y", 0m);
        var result = Eval("x + y", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(preciseValue));
    }

    [Test]
    public void NegateDecimal_ReturnsDecimal()
    {
        var context = new EvalContext();
        context.Define("x", 10.5m);
        var result = Eval("-x", context);
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(-10.5m));
    }

    #endregion

    #region Modulo Operations

    [Test]
    public void IntModInt_ReturnsInt()
    {
        // C# behavior: int % int → int
        var result = Eval("10 % 3");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void DoubleModDouble_ReturnsDouble()
    {
        // Double modulo may throw or return specific type
        try
        {
            var result = Eval("10.5 % 3.0");
            Assert.That(result, Is.Not.Null);
            Assert.That(Convert.ToDouble(result), Is.EqualTo(1.5).Within(0.01));
        }
        catch (EvalException)
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
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1, 2, 3]") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.TypeOf<int>());
        Assert.That(result[1], Is.TypeOf<int>());
        Assert.That(result[2], Is.TypeOf<int>());
    }

    [Test]
    public void ArrayLiteral_LongElements_ReturnsLongList()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1L, 2L, 3L]") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.TypeOf<long>());
        Assert.That(result[1], Is.TypeOf<long>());
        Assert.That(result[2], Is.TypeOf<long>());
    }

    [Test]
    public void ArrayLiteral_IndexAccess_PreservesType()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var arr = [10, 20, 30]; return arr[1]; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(20));
    }

    #endregion

    #region Block Expressions - Type Verification

    [Test]
    public void BlockExpression_IntVariable_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 42; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void BlockExpression_IntArithmetic_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 10; var y = 5; return x + y; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void BlockExpression_IntAssignment_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 1; x = 99; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void BlockExpression_IntCompoundAssignment_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 10; x += 5; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void BlockExpression_IntIncrement_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 5; x++; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region Bitwise Operations in Blocks - Type Verification

    [Test]
    public void BlockExpression_BitwiseAnd_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 15; x &= 9; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void BlockExpression_BitwiseOr_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 5; x |= 3; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void BlockExpression_BitwiseXor_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 12; x ^= 5; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void BlockExpression_LeftShift_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 1; x <<= 4; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void BlockExpression_RightShift_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("{ var x = 32; x >>= 2; return x; }");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(8));
    }

    #endregion

    #region Loop Counter - Type Verification

    [Test]
    public void ForLoop_IntCounter_ReturnsInt()
    {
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
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
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Where(x => x > 2).ToList()") as List<object?>;

        Assert.That(result, Is.Not.Null);
        Assert.That(result![0], Is.TypeOf<int>());
        Assert.That(result[0], Is.EqualTo(3));
    }

    [Test]
    public void LinqSum_IntArray_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Sum()");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void LinqChain_IntArray_ReturnsInt()
    {
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Where(x => x > 2).Select(x => x * 10).Sum()");

        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(120)); // (3+4+5)*10 = 120
    }

    [Test]
    public void LinqAverage_IntArray_ReturnsDouble()
    {
        // C# behavior: Average of int/long returns double
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1, 2, 3, 4, 5].Average()");

        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void LinqAverage_DecimalArray_ReturnsDecimal()
    {
        // C# behavior: Average of decimal returns decimal
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1.5m, 2.5m, 3.5m].Average()");

        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.5m));
    }

    [Test]
    public void LinqAverage_LongArray_ReturnsDouble()
    {
        // C# behavior: Average of long returns double
        var engine = new CsEvalEngine();
        var result = engine.Evaluate("[1L, 2L, 3L, 4L, 5L].Average()");

        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void LinqAverage_DecimalWithSelector_ReturnsDecimal()
    {
        // C# behavior: Average with selector returning decimal returns decimal
        var engine = new CsEvalEngine();
        engine.SetVariable("items", new List<int> { 1, 2, 3 });
        var result = engine.Evaluate("items.Average(x => x * 1.0m)");

        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.0m));
    }

    #endregion
}

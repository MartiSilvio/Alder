namespace CsEval.Test.Types;

/// <summary>
/// Comprehensive numeric tests to ensure CsEval handles all numeric types correctly.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NumericTests(CompilationMode mode)
{
    // Literals
    [TestCase("42", TestName = "Literal_Int")]
    [TestCase("0", TestName = "Literal_Zero")]
    [TestCase("-42", TestName = "Literal_NegativeInt")]
    [TestCase("9223372036854775807", TestName = "Literal_LongMax")]
    [TestCase("42L", TestName = "Literal_LongSuffix")]
    [TestCase("3.14f", TestName = "Literal_Float")]
    [TestCase("3.14m", TestName = "Literal_Decimal")]
    [TestCase("42m", TestName = "Literal_IntAsDecimal")]
    [TestCase("3.14", TestName = "Literal_Double")]
    [TestCase("0.5", TestName = "Literal_DoubleLeadingZero")]
    [TestCase("-3.14", TestName = "Literal_NegativeDouble")]
    [TestCase("0.00001", TestName = "Literal_SmallDouble")]
    // Arithmetic - same types
    [TestCase("5 + 3", TestName = "Arithmetic_IntPlusInt")]
    [TestCase("10 - 4", TestName = "Arithmetic_IntMinusInt")]
    [TestCase("6 * 7", TestName = "Arithmetic_IntTimesInt")]
    [TestCase("10 / 4", TestName = "Arithmetic_IntDivInt")]
    [TestCase("5L + 3L", TestName = "Arithmetic_LongPlusLong")]
    [TestCase("5 + 3L", TestName = "Arithmetic_IntPlusLong")]
    [TestCase("1.5 + 2.5", TestName = "Arithmetic_DoublePlusDouble")]
    [TestCase("2.5 * 4.0", TestName = "Arithmetic_DoubleTimesDouble")]
    // Arithmetic - mixed types
    [TestCase("5 + 2.5", TestName = "Arithmetic_IntPlusDouble")]
    [TestCase("2.5 + 5", TestName = "Arithmetic_DoublePlusInt")]
    // Comparisons
    [TestCase("3.14 == 3.14", TestName = "Compare_DoubleEquals")]
    [TestCase("5 < 5.5", TestName = "Compare_IntLessThanDouble")]
    [TestCase("5.5 > 5", TestName = "Compare_DoubleGreaterThanInt")]
    [TestCase("10 >= 10", TestName = "Compare_GreaterOrEqual")]
    [TestCase("10 <= 10", TestName = "Compare_LessOrEqual")]
    [TestCase("5 != 6", TestName = "Compare_NotEqual")]
    // Modulo
    [TestCase("10 % 3", TestName = "Modulo_IntModInt")]
    [TestCase("10.5 % 3.0", TestName = "Modulo_DoubleModDouble")]
    // Division precision
    [TestCase("7 / 3", TestName = "Division_IntTruncates")]
    [TestCase("7.0 / 3.0", TestName = "Division_DoublePrecision")]
    // Floating-point precision
    [TestCase("0.1 + 0.2", TestName = "Precision_PointOnePointTwo")]
    // Negation
    [TestCase("-42", TestName = "Negate_Int")]
    [TestCase("-3.14", TestName = "Negate_Double")]
    [TestCase("-3.14m", TestName = "Negate_Decimal")]
    // Bitwise
    [TestCase("15 & 9", TestName = "Bitwise_And")]
    [TestCase("5 | 3", TestName = "Bitwise_Or")]
    [TestCase("12 ^ 5", TestName = "Bitwise_Xor")]
    [TestCase("1 << 4", TestName = "Bitwise_LeftShift")]
    [TestCase("32 >> 2", TestName = "Bitwise_RightShift")]
    // LINQ
    [TestCase("new[] { 1, 2, 3 }.Select(x => x * 2).ToList()", TestName = "Linq_Select")]
    [TestCase("new[] { 1, 2, 3, 4, 5 }.Where(x => x > 2).ToList()", TestName = "Linq_Where")]
    [TestCase("new[] { 1, 2, 3, 4, 5 }.Sum()", TestName = "Linq_Sum_IntArray")]
    [TestCase("new[] { 1, 2, 3, 4, 5 }.Where(x => x > 2).Select(x => x * 10).Sum()", TestName = "Linq_WhereSelectSum")]
    [TestCase("new[] { 1, 2, 3, 4, 5 }.Average()", TestName = "Linq_Average_IntArray")]
    [TestCase("new[] { 1.5m, 2.5m, 3.5m }.Average()", TestName = "Linq_Average_DecimalArray")]
    [TestCase("new[] { 1L, 2L, 3L, 4L, 5L }.Average()", TestName = "Linq_Average_LongArray")]
    [TestCase("Enumerable.Range(1, 5).ToList()", TestName = "Linq_Range_ToList")]
    [TestCase("Enumerable.Range(1, 5).ToArray()", TestName = "Linq_Range_ToArray")]
    [TestCase("Enumerable.Range(1, 5).Where(x => x > 2).ToList()", TestName = "Linq_Range_Where")]
    [TestCase("Enumerable.Range(1, 5).Select(x => x * 2).ToList()", TestName = "Linq_Range_Select")]
    [TestCase("Enumerable.Range(1, 5).Take(3).ToList()", TestName = "Linq_Range_Take")]
    [TestCase("Enumerable.Range(1, 5).Skip(2).ToList()", TestName = "Linq_Range_Skip")]
    [TestCase("Enumerable.Range(1, 5).OrderByDescending(x => x).ToList()", TestName = "Linq_Range_OrderByDescending")]
    [TestCase("Enumerable.Range(1, 5).Reverse().ToList()", TestName = "Linq_Range_Reverse")]
    [TestCase("Enumerable.Range(1, 5).Distinct().ToList()", TestName = "Linq_Range_Distinct")]
    [TestCase("Enumerable.Repeat(42, 3).ToList()", TestName = "Linq_Repeat_Int")]
    [TestCase("Enumerable.Repeat(\"x\", 3).ToList()", TestName = "Linq_Repeat_String")]
    [TestCase("new[] { 1, 2, 3 }.Where(x => x > 1).ToList()", TestName = "Linq_Array_Where")]
    [TestCase("new[] { 1, 2, 3 }.Select(x => x.ToString()).ToList()", TestName = "Linq_Array_SelectToString")]
    [TestCase("new[] { 1, 2, 3 }.Sum()", TestName = "Linq_Array_Sum")]
    [TestCase("new[] { 1.5, 2.5, 3.5 }.Sum()", TestName = "Linq_Sum_DoubleArray")]
    [TestCase("new[] { 1m, 2m, 3m }.Sum()", TestName = "Linq_Sum_DecimalArray")]
    [TestCase("new[] { 1L, 2L, 3L }.Sum()", TestName = "Linq_Sum_LongArray")]
    [TestCase("Enumerable.Range(1, 5).Sum()", TestName = "Linq_Range_Sum")]
    [TestCase("Enumerable.Range(1, 5).Count()", TestName = "Linq_Range_Count")]
    [TestCase("Enumerable.Range(1, 5).Min()", TestName = "Linq_Range_Min")]
    [TestCase("Enumerable.Range(1, 5).Max()", TestName = "Linq_Range_Max")]
    [TestCase("Enumerable.Range(1, 5).Average()", TestName = "Linq_Range_Average")]
    [TestCase("Enumerable.Range(1, 5).First()", TestName = "Linq_Range_First")]
    [TestCase("Enumerable.Range(1, 5).Last()", TestName = "Linq_Range_Last")]
    [TestCase("Enumerable.Range(1, 5).Any()", TestName = "Linq_Range_Any")]
    [TestCase("Enumerable.Range(1, 5).Any(x => x > 3)", TestName = "Linq_Range_AnyPredicate")]
    [TestCase("Enumerable.Range(1, 5).All(x => x > 0)", TestName = "Linq_Range_All")]
    [TestCase("Enumerable.Range(1, 5).Contains(3)", TestName = "Linq_Range_Contains")]
    [TestCase("Enumerable.Repeat(5, 4).Sum()", TestName = "Linq_Repeat_Sum")]
    [TestCase("Enumerable.Range(1, 3).Select(x => x * 1.5).Sum()", TestName = "Linq_Range_SelectDoubleSum")]
    public async Task MatchesCSharp(string expr)
        => await TestHelpers.RunCSharpParityTestAsync(expr, mode);

    // Tests requiring variables (can't parity test against Roslyn)
    [Test]
    public void IntTimesInt_FromVariable_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("x * 2");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntTimesLong_ReturnsLong()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("x * 2L");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void IntTimesDouble_ReturnsDouble()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 5);
        var result = engine.Evaluate("x * 2.5");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(12.5));
    }

    [Test]
    public void SByte_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (sbyte)10);
        engine.SetVariable("y", (sbyte)5);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void Short_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (short)1000);
        engine.SetVariable("y", (short)234);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(1234));
    }

    [Test]
    public void Int_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 100000);
        engine.SetVariable("y", 23456);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(123456));
    }

    [Test]
    public void Long_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10000000000L);
        engine.SetVariable("y", 2345678901L);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<long>());
        Assert.That(result, Is.EqualTo(12345678901L));
    }

    [Test]
    public void Byte_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (byte)200);
        engine.SetVariable("y", (byte)55);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(255));
    }

    [Test]
    public void UShort_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (ushort)60000);
        engine.SetVariable("y", (ushort)5535);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(65535));
    }

    [Test]
    public void UInt_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 4000000000u);
        engine.SetVariable("y", 294967295u);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<uint>());
        Assert.That(result, Is.EqualTo(4294967295u));
    }

    [Test]
    public void ULong_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10000000000UL);
        engine.SetVariable("y", 5000000000UL);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<ulong>());
        Assert.That(result, Is.EqualTo(15000000000UL));
    }

    [Test]
    public void Float_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25f);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(15.75f).Within(0.001f));
    }

    [Test]
    public void Double_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5d);
        engine.SetVariable("y", 5.25d);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That(result, Is.EqualTo(15.75));
    }

    [Test]
    public void Decimal_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5m);
        engine.SetVariable("y", 5.25m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.75m));
    }

    [Test]
    public void BytePlusShort_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", (byte)100);
        engine.SetVariable("y", (short)200);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(300));
    }

    [Test]
    public void IntPlusFloat_ReturnsFloat()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10);
        engine.SetVariable("y", 5.5f);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(15.5f).Within(0.001f));
    }

    [Test]
    public void FloatPlusDouble_ReturnsDouble()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25d);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(15.75).Within(0.001));
    }

    [Test]
    public void IntPlusDecimal_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10);
        engine.SetVariable("y", 5.5m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void FloatPlusDecimal_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25m);
        Assert.That(() => engine.Evaluate("x + y"), Throws.TypeOf<CsEvalException>());
    }

    [Test]
    public void DoublePlusDecimal_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5d);
        engine.SetVariable("y", 5.25m);
        Assert.That(() => engine.Evaluate("x + y"), Throws.TypeOf<CsEvalException>());
    }

    [Test]
    public void IntEqualsLong_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42);
        var result = engine.Evaluate("x == 42");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LongEqualsInt_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 42L);
        var result = engine.Evaluate("x == 42");
        Assert.That(result, Is.True);
    }

    // Contains with different types
    [Test]
    public void Contains_IntListWithLongLiteral_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<object?> { 1, 2, 3 });
        var result = engine.Evaluate("numbers.Contains(2)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_LongListWithIntVariable_MatchesCSharpSemantics()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<object?> { 1L, 2L, 3L });
        engine.SetVariable("search", 2);
        var result = engine.Evaluate("numbers.Contains(search)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_DoubleListWithDoubleLiteral_Works()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<object?> { 1.5, 2.5, 3.5 });
        var result = engine.Evaluate("numbers.Contains(2.5)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_DecimalListWithDoubleLiteral_MatchesCSharpSemantics()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<object?> { 1.5m, 2.5m, 3.5m });
        var result = engine.Evaluate("numbers.Contains(2.5)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_MixedNumericTypes_MatchesCSharpSemantics()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("numbers", new List<object?> { 1, 2L, 3.0, 4.0f });
        Assert.That(engine.Evaluate("numbers.Contains(1)"), Is.True);
        Assert.That(engine.Evaluate("numbers.Contains(2)"), Is.False);
        Assert.That(engine.Evaluate("numbers.Contains(3)"), Is.False);
        Assert.That(engine.Evaluate("numbers.Contains(4)"), Is.False);
    }

    // Precision tests with variables
    [Test]
    public void Decimal_PointOneIsExact()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 0.1m);
        engine.SetVariable("y", 0.2m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(0.3m));
    }

    [Test]
    public void Double_CompoundingError_RepeatedAddition()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("sum", 0.0);
        for (int i = 0; i < 10; i++)
        {
            var current = (double)engine.Evaluate("sum")!;
            engine.SetVariable("sum", current + 0.01);
        }
        var result = (double)engine.Evaluate("sum")!;
        Assert.That(result, Is.Not.EqualTo(0.1));
        Assert.That(result, Is.EqualTo(0.09999999999999999).Within(1e-16));
    }

    [Test]
    public void Decimal_NoCompoundingError_RepeatedAddition()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("sum", 0.0m);
        for (int i = 0; i < 10; i++)
        {
            var current = (decimal)engine.Evaluate("sum")!;
            engine.SetVariable("sum", current + 0.01m);
        }
        var result = engine.Evaluate("sum");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(0.1m));
    }

    [Test]
    public void Double_DivisionMultiplication_OneThird()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 1.0 / 3.0);
        var result = engine.Evaluate("x * 3");
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-15));
    }

    [Test]
    public void Decimal_DivisionMultiplication_OneThird()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("third", 1.0m / 3.0m);
        var result = engine.Evaluate("third * 3");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That((decimal)result!, Is.EqualTo(1.0m).Within(0.0000000001m));
    }

    [Test]
    public void LargeLongArithmetic_NoOverflow()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", long.MaxValue - 1);
        var result = engine.Evaluate("x + 1");
        Assert.That(result, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void Decimal_FinancialCalculation_InterestRate()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("principal", 10000.00m);
        engine.SetVariable("rate", 0.0525m);
        var result = engine.Evaluate("principal * rate");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(525.00m));
    }

    [Test]
    public void Double_FinancialCalculation_MayHaveError()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("principal", 10000.00d);
        engine.SetVariable("rate", 0.0525d);
        var result = engine.Evaluate("principal * rate");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(525.0).Within(1e-10));
    }

    [Test]
    public void Double_LosesPrecisionAt17Digits()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 12345678901234567.0);
        engine.SetVariable("y", 1.0);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void Decimal_Preserves28Digits()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 1234567890123456789012345678m);
        engine.SetVariable("y", 1m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(1234567890123456789012345679m));
    }

    [Test]
    public void DecimalPlusDecimal_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5m);
        engine.SetVariable("y", 5.25m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.75m));
    }

    [Test]
    public void DecimalPlusLong_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5m);
        var result = engine.Evaluate("x + 5");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void LongPlusDecimal_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5m);
        var result = engine.Evaluate("5 + x");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(15.5m));
    }

    [Test]
    public void DecimalTimesDecimal_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 3.5m);
        engine.SetVariable("y", 2.0m);
        var result = engine.Evaluate("x * y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(7.0m));
    }

    [Test]
    public void DecimalDivideDecimal_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.0m);
        engine.SetVariable("y", 4.0m);
        var result = engine.Evaluate("x / y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.5m));
    }

    [Test]
    public void DecimalMinusDecimal_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.75m);
        engine.SetVariable("y", 5.25m);
        var result = engine.Evaluate("x - y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(5.5m));
    }

    [Test]
    public void DecimalModDecimal_ReturnsDecimal()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 10.5m);
        engine.SetVariable("y", 3.0m);
        var result = engine.Evaluate("x % y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(1.5m));
    }

    [Test]
    public void DecimalPrecision_PreservedHighPrecisionValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 1.1234567890123456789m);
        engine.SetVariable("y", 1.0m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.1234567890123456789m));
    }

    [Test]
    public void DecimalPrecision_NotLostToDouble()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 5.5m);
        var result = engine.Evaluate("-x");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(-5.5m));
    }

    // CsEval-specific syntax tests (can't parity test - [1,2,3] syntax not in C#)
    [Test]
    public void ArrayLiteral_IntElements_ReturnsIntList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.TypeOf<List<int>>());
    }

    [Test]
    public void ArrayLiteral_LongElements_ReturnsLongList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1L, 2L, 3L]");
        Assert.That(result, Is.TypeOf<List<long>>());
    }

    [Test]
    public void ArrayLiteral_IndexAccess_PreservesType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var arr = [10, 20, 30]; return arr[1]; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void BlockExpression_IntVariable_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 42; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void BlockExpression_IntArithmetic_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 10; var y = 5; return x + y; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void BlockExpression_IntAssignment_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 1; x = 99; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void BlockExpression_IntCompoundAssignment_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 10; x += 5; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void BlockExpression_IntIncrement_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 5; x++; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void BlockExpression_BitwiseAnd_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 15; x &= 9; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void BlockExpression_BitwiseOr_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 5; x |= 3; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(7));
    }

    [Test]
    public void BlockExpression_BitwiseXor_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 12; x ^= 5; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void BlockExpression_LeftShift_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 1; x <<= 4; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(16));
    }

    [Test]
    public void BlockExpression_RightShift_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var x = 32; x >>= 2; return x; }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(8));
    }

    [Test]
    public void ForLoop_IntCounter_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(@"
        {
            var sum = 0;
            for (var i = 0; i < 5; i++) {
                sum += i;
            }
            return sum;
        }");
        Assert.That(result, Is.TypeOf<int>());
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void WhileLoop_IntCounter_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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
        Assert.That(result, Is.EqualTo(15));
    }

    [Test]
    public void ForEachLoop_IntArraySum_ReturnsInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
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

    #region ECMA-334 Edge Cases - Numeric Boundaries

    // Boundary values for signed types
    [TestCase("(sbyte)-128", (sbyte)-128, TestName = "Boundary_SByte_MinValue")]
    [TestCase("(sbyte)127", (sbyte)127, TestName = "Boundary_SByte_MaxValue")]
    [TestCase("(short)-32768", (short)-32768, TestName = "Boundary_Short_MinValue")]
    [TestCase("(short)32767", (short)32767, TestName = "Boundary_Short_MaxValue")]
    [TestCase("-2147483648", -2147483648, TestName = "Boundary_Int_MinValue")]
    [TestCase("2147483647", 2147483647, TestName = "Boundary_Int_MaxValue")]
    [TestCase("-9223372036854775808L", -9223372036854775808L, TestName = "Boundary_Long_MinValue")]
    [TestCase("9223372036854775807L", 9223372036854775807L, TestName = "Boundary_Long_MaxValue")]
    public async Task Boundary_SignedTypes_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Boundary values for unsigned types
    [TestCase("(byte)0", (byte)0, TestName = "Boundary_Byte_MinValue")]
    [TestCase("(byte)255", (byte)255, TestName = "Boundary_Byte_MaxValue")]
    [TestCase("(ushort)0", (ushort)0, TestName = "Boundary_UShort_MinValue")]
    [TestCase("(ushort)65535", (ushort)65535, TestName = "Boundary_UShort_MaxValue")]
    [TestCase("0u", 0u, TestName = "Boundary_UInt_MinValue")]
    [TestCase("4294967295u", 4294967295u, TestName = "Boundary_UInt_MaxValue")]
    [TestCase("0UL", 0UL, TestName = "Boundary_ULong_MinValue")]
    [TestCase("18446744073709551615UL", 18446744073709551615UL, TestName = "Boundary_ULong_MaxValue")]
    public async Task Boundary_UnsignedTypes_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Large hex literals
    [TestCase("0xFFFFFFFF", 0xFFFFFFFF, TestName = "HexLiteral_MaxUInt")]
    [TestCase("0xFFFF_FFFF_FFFF_FFFF", 0xFFFF_FFFF_FFFF_FFFF, TestName = "HexLiteral_MaxULong")]
    [TestCase("0x7FFFFFFF", 0x7FFFFFFF, TestName = "HexLiteral_MaxInt")]
    [TestCase("0x7FFF_FFFF_FFFF_FFFF", 0x7FFF_FFFF_FFFF_FFFF, TestName = "HexLiteral_MaxLong")]
    public async Task HexLiterals_LargeBoundaries_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Binary literals at boundaries
    [TestCase("0b11111111", 255, TestName = "BinaryLiteral_Byte_Max")]
    [TestCase("0b1111_1111_1111_1111", 65535, TestName = "BinaryLiteral_UShort_Max")]
    public async Task BinaryLiterals_Boundaries_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    // Char to int conversions
    [TestCase("(int)'\\0'", 0, TestName = "CharToInt_NullChar")]
    [TestCase("(int)'\\x00'", 0, TestName = "CharToInt_HexNull")]
    [TestCase("(int)'\\uFFFF'", 65535, TestName = "CharToInt_MaxUnicode")]
    public async Task CharToInt_Boundaries_MatchesCSharp(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #endregion

    #region ECMA-334 Edge Cases - Variable Shadowing

    // Variable shadowing should NOT be allowed
    [Test]
    public void VariableShadowing_InNestedBlock_ShouldThrow()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // In C#, this is a compile error - can't redeclare in nested scope
        Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            var x = 1;
            {
                var x = 2;
            }
            return x;
        }"));
    }

    // Variables should be scoped correctly
    [Test]
    public void VariableScope_InNestedBlock_ShouldBeIsolated()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // y is declared inside the block and should not be accessible outside
        Assert.Throws<CsEvalException>(() => engine.Evaluate(@"
        {
            {
                var y = 5;
            }
            return y;
        }"));
    }

    #endregion

    #region ECMA-334 Edge Cases - Empty String Type Check

    [Test]
    public async Task EmptyString_IsString_True()
    {
        var expr = "\"\" is string";
        await TestHelpers.RunCSharpParityTestAsync(expr, true, mode);
    }

    [Test]
    public async Task EmptyString_IsObject_True()
    {
        var expr = "\"\" is object";
        await TestHelpers.RunCSharpParityTestAsync(expr, true, mode);
    }

    #endregion
}

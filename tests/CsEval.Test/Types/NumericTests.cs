namespace CsEval.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5.2 -- Integer literals, §6.4.5.3 -- Real literals,
/// §12.9 -- Arithmetic operators, §12.4.7.3 -- Binary numeric promotion,
/// §12.12 -- Relational operators, §12.13 -- Bitwise operators.
/// Comprehensive numeric tests covering literal parsing, arithmetic with type promotion,
/// comparisons, precision semantics, LINQ aggregation, and block expression evaluation.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class NumericTests(CompilationMode mode)
{
    #region ECMA-334 §12.4.7.3 -- Numeric Type Promotion with Variables
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

    #endregion

    #region ECMA-334 §12.4.7.3 -- Cross-Type Arithmetic and Incompatible Operands

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

    #endregion

    #region ECMA-334 §12.12.7 -- Cross-Type Equality and Contains Semantics

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

    #endregion

    #region ECMA-334 §8.3.7 -- Floating-Point and Decimal Precision
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

    #endregion

    #region CsEval-Specific -- Array Literal and Block Expression Syntax
    [Test]
    public void ArrayLiteral_IntElements_ReturnsIntList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.TypeOf<int[]>());
    }

    [Test]
    public void ArrayLiteral_LongElements_ReturnsLongList()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("[1L, 2L, 3L]");
        Assert.That(result, Is.TypeOf<long[]>());
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

    #endregion

    #region ECMA-334 §12.21 -- Compound Assignment in Block Expressions

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

    #endregion

    #region ECMA-334 §13.9 -- Loop Statements (for, while, foreach)

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

    #endregion

    #region ECMA-334 §12.4.7.3 - Binary Numeric Promotion

    // These 8 tests use Type expectedType parameter which cannot be expressed in TestCaseData,
    // so they stay inline as [TestCase] attributes.
    [TestCase("1 + 1L", typeof(long), TestName = "NumericPromotion_IntPlusLong_IsLong")]
    [TestCase("1L + 1", typeof(long), TestName = "NumericPromotion_LongPlusInt_IsLong")]
    [TestCase("1 + 1.0", typeof(double), TestName = "NumericPromotion_IntPlusDouble_IsDouble")]
    [TestCase("1.0f + 1.0", typeof(double), TestName = "NumericPromotion_FloatPlusDouble_IsDouble")]
    [TestCase("1.0f + 1", typeof(float), TestName = "NumericPromotion_FloatPlusInt_IsFloat")]
    [TestCase("1L + 1.0f", typeof(float), TestName = "NumericPromotion_LongPlusFloat_IsFloat")]
    [TestCase("(byte)1 + (byte)2", typeof(int), TestName = "NumericPromotion_BytePlusByte_IsInt")]
    [TestCase("(short)1 + (short)2", typeof(int), TestName = "NumericPromotion_ShortPlusShort_IsInt")]
    public async Task NumericPromotion_ResultType(string expr, Type expectedType)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result?.GetType(), Is.EqualTo(expectedType), $"CsEval type mismatch for: {expr}");
        Assert.That(csharpResult?.GetType(), Is.EqualTo(expectedType), $"C# type mismatch for: {expr}");
    }

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

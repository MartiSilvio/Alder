using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Types;

/// <summary>
/// ECMA-334 §6.4.5.2 -- Integer literals, §6.4.5.3 -- Real literals,
/// §12.9 -- Arithmetic operators, §12.4.7.3 -- Binary numeric promotion,
/// §12.12 -- Relational operators, §12.13 -- Bitwise operators.
/// Engine-only numeric tests for behaviors that cannot be expressed as .csx parity tests.
/// Parity tests migrated to TestData/Types/Numeric/*.csx and Parity/NumericTests.cs.
/// </summary>
[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class NumericTests(CompilationMode mode)
{

    [Test]
    public void IntPlusFloat_ReturnsFloat()
    {
        // Engine-only: uses .Within() float tolerance assertion
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10);
        engine.SetVariable("y", 5.5f);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<float>());
        Assert.That((float)result!, Is.EqualTo(15.5f).Within(0.001f));
    }

    [Test]
    public void FloatPlusDouble_ReturnsDouble()
    {
        // Engine-only: uses .Within() float tolerance assertion
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25d);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(15.75).Within(0.001));
    }

    [Test]
    public void Double_DivisionMultiplication_OneThird()
    {
        // Engine-only: uses .Within() float tolerance assertion
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 1.0 / 3.0);
        var result = engine.Evaluate("x * 3");
        Assert.That((double)result!, Is.EqualTo(1.0).Within(1e-15));
    }

    [Test]
    public void Decimal_DivisionMultiplication_OneThird()
    {
        // Engine-only: uses .Within() decimal tolerance assertion
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("third", 1.0m / 3.0m);
        var result = engine.Evaluate("third * 3");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That((decimal)result!, Is.EqualTo(1.0m).Within(0.0000000001m));
    }

    [Test]
    public void Double_FinancialCalculation_MayHaveError()
    {
        // Engine-only: uses .Within() float tolerance assertion
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("principal", 10000.00d);
        engine.SetVariable("rate", 0.0525d);
        var result = engine.Evaluate("principal * rate");
        Assert.That(result, Is.TypeOf<double>());
        Assert.That((double)result!, Is.EqualTo(525.0).Within(1e-10));
    }

    [Test]
    public void Double_LosesPrecisionAt17Digits()
    {
        // Engine-only: tests precision behavior, no exact expected value
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 12345678901234567.0);
        engine.SetVariable("y", 1.0);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<double>());
    }

    [Test]
    public void DecimalPrecision_PreservedHighPrecisionValue()
    {
        // Engine-only: uses SetVariable with high-precision decimal
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 1.1234567890123456789m);
        engine.SetVariable("y", 1.0m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(2.1234567890123456789m));
    }

    [Test]
    public void DecimalPrecision_NotLostToDouble()
    {
        // Engine-only: uses SetVariable with high-precision decimal
        var engine = TestEngineFactory.Create(mode);
        var preciseValue = 12345678901234567890.12345678m;
        engine.SetVariable("x", preciseValue);
        engine.SetVariable("y", 0m);
        var result = engine.Evaluate("x + y");
        Assert.That(result, Is.TypeOf<decimal>());
        Assert.That(result, Is.EqualTo(preciseValue));
    }



    [Test]
    public void FloatPlusDecimal_Throws()
    {
        // Engine-only: tests exception throwing behavior
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10.5f);
        engine.SetVariable("y", 5.25m);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x + y"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }

    [Test]
    public void DoublePlusDecimal_Throws()
    {
        // Engine-only: tests exception throwing behavior
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", 10.5d);
        engine.SetVariable("y", 5.25m);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("x + y"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0019));
    }



    [Test]
    public void Double_CompoundingError_RepeatedAddition()
    {
        // Engine-only: multi-step iterative engine state (repeated SetVariable in loop)
        var engine = TestEngineFactory.Create(mode);
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
        // Engine-only: multi-step iterative engine state (repeated SetVariable in loop)
        var engine = TestEngineFactory.Create(mode);
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
    public void Contains_IntListWithLongLiteral_Works()
    {
        // Engine-only: SetVariable with List<object?> (non-serializable for Roslyn)
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<object?> { 1, 2, 3 });
        var result = engine.Evaluate("numbers.Contains(2)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_LongListWithIntVariable_MatchesCSharpSemantics()
    {
        // Engine-only: SetVariable with List<object?> (non-serializable for Roslyn)
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<object?> { 1L, 2L, 3L });
        engine.SetVariable("search", 2);
        var result = engine.Evaluate("numbers.Contains(search)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_DoubleListWithDoubleLiteral_Works()
    {
        // Engine-only: SetVariable with List<object?> (non-serializable for Roslyn)
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<object?> { 1.5, 2.5, 3.5 });
        var result = engine.Evaluate("numbers.Contains(2.5)");
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_DecimalListWithDoubleLiteral_MatchesCSharpSemantics()
    {
        // Engine-only: SetVariable with List<object?> (non-serializable for Roslyn)
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<object?> { 1.5m, 2.5m, 3.5m });
        var result = engine.Evaluate("numbers.Contains(2.5)");
        Assert.That(result, Is.False);
    }

    [Test]
    public void Contains_MixedNumericTypes_MatchesCSharpSemantics()
    {
        // Engine-only: SetVariable with List<object?> (non-serializable for Roslyn)
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("numbers", new List<object?> { 1, 2L, 3.0, 4.0f });
        Assert.That(engine.Evaluate("numbers.Contains(1)"), Is.True);
        Assert.That(engine.Evaluate("numbers.Contains(2)"), Is.False);
        Assert.That(engine.Evaluate("numbers.Contains(3)"), Is.False);
        Assert.That(engine.Evaluate("numbers.Contains(4)"), Is.False);
    }





    [Test]
    public void VariableShadowing_InNestedBlock_IsAllowed()
    {
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate(@"
        {
            var x = 1;
            {
                var x = 2;
            }
            return x;
        }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void VariableScope_InNestedBlock_ShouldBeIsolated()
    {
        // Engine-only: tests AlderException error behavior
        var engine = TestEngineFactory.Create(mode);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate(@"
        {
            {
                var y = 5;
            }
            return y;
        }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }



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
        // Engine-only: TestCase attribute cannot carry System.Type in TestCaseData
        var engine = TestEngineFactory.Create(mode);
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result?.GetType(), Is.EqualTo(expectedType), $"Alder type mismatch for: {expr}");
        Assert.That(csharpResult?.GetType(), Is.EqualTo(expectedType), $"C# type mismatch for: {expr}");
    }

}

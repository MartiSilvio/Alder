using CsEval.TestData.Data;

namespace CsEval.Test.Operators;

/// <summary>
/// ECMA-334 §10.3 — Explicit conversions (cast expressions),
/// §10.3.7 — Unboxing conversions, §10.3.2 — Explicit numeric conversions.
/// Tests explicit casts, nullable casts, unboxing rules, and conversion edge cases.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CastTests(CompilationMode mode)
{
    [TestCaseSource(typeof(CastData), nameof(CastData.ValueCases))]
    public async Task Eval_Cast(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(CastData), nameof(CastData.NullCastCases))]
    public async Task Eval_Cast_ToNull(string expr, object? expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(CastData), nameof(CastData.InvalidCastErrorCases))]
    public async Task Eval_Cast_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Catch<Exception>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    [TestCaseSource(typeof(CastData), nameof(CastData.InvalidUnboxingErrorCases))]
    public async Task Eval_Cast_InvalidUnboxing_ShouldThrow(string expr)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        Assert.Throws<InvalidCastException>(() => engine.Evaluate(expr));
        await Assert.ThatAsync(async () => await TestHelpers.EvaluateCSharpAsync(expr), Throws.Exception);
    }

    [TestCaseSource(typeof(CastData), nameof(CastData.ValidUnboxingCases))]
    public async Task Eval_Cast_ValidUnboxing(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(CastData), nameof(CastData.NumericConversionCases))]
    public async Task Eval_Cast_NumericConversion_NotUnboxing(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(CastData), nameof(CastData.CharConversionCases))]
    public async Task Conversion_CharExplicit(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(CastData), nameof(CastData.FloatTruncationCases))]
    public async Task Conversion_FloatTruncation(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    [TestCaseSource(typeof(CastData), nameof(CastData.NarrowingOverflowCases))]
    public async Task Conversion_NarrowingOverflow(string expr, object expected)
        => await TestHelpers.RunCSharpParityTestAsync(expr, expected, mode);

    #region Cast with Non-Keyword Class Types

    [Test]
    public void Cast_ClassType_Compatible()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", (object)new ArgumentException("test"));
        var result = engine.Evaluate("(Exception)obj");
        Assert.That(result, Is.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Cast_ClassType_ExactMatch()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", (object)new Exception("test"));
        var result = engine.Evaluate("(Exception)obj");
        Assert.That(result, Is.InstanceOf<Exception>());
        Assert.That(((Exception)result!).Message, Is.EqualTo("test"));
    }

    [Test]
    public void Cast_ClassType_Incompatible_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", (object)"hello");
        Assert.Throws<InvalidCastException>(() => engine.Evaluate("(Exception)obj"));
    }

    [Test]
    public void Cast_ClassType_NullValue()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("obj", null);
        var result = engine.Evaluate("(Exception)obj");
        Assert.That(result, Is.Null);
    }

    #endregion

    #region ECMA-334 §10.3.7 — Unboxing Edge Cases

    [Test]
    public async Task Unboxing_ExactTypeMatch_Required()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("boxed", (object)42);

        Assert.That(engine.Evaluate("(int)boxed"), Is.EqualTo(42));
        Assert.Catch<InvalidCastException>(() => engine.Evaluate("(long)boxed"));

        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(int)(object)42");
        Assert.That(csharpResult, Is.EqualTo(42));
    }

    [Test]
    public void Unboxing_NullableFromBoxed()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("boxed", (object)42);

        var result = engine.Evaluate("(int?)boxed");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion
}

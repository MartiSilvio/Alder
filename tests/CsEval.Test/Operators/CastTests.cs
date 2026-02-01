namespace CsEval.Test.Operators;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class CastTests(CompilationMode mode)
{
    [TestCase("(int)3.7", 3, TestName = "Cast_DoubleToInt")]
    [TestCase("(int)3.9", 3, TestName = "Cast_DoubleToInt_Truncates")]
    [TestCase("(double)5", 5.0, TestName = "Cast_IntToDouble")]
    [TestCase("(long)42", 42L, TestName = "Cast_IntToLong")]
    [TestCase("(int)100L", 100, TestName = "Cast_LongToInt")]
    [TestCase("(float)3.14", 3.14f, TestName = "Cast_DoubleToFloat")]
    [TestCase("(double)3.14f", (double)3.14f, TestName = "Cast_FloatToDouble")]
    [TestCase("(byte)255", (byte)255, TestName = "Cast_IntToByte")]
    [TestCase("(sbyte)127", (sbyte)127, TestName = "Cast_IntToSbyte")]
    [TestCase("(short)1000", (short)1000, TestName = "Cast_IntToShort")]
    [TestCase("(ushort)50000", (ushort)50000, TestName = "Cast_IntToUshort")]
    [TestCase("(uint)100", 100u, TestName = "Cast_IntToUint")]
    [TestCase("(ulong)100", 100ul, TestName = "Cast_IntToUlong")]
    [TestCase("(int)'A'", 65, TestName = "Cast_CharToInt")]
    [TestCase("(char)65", 'A', TestName = "Cast_IntToChar")]
    [TestCase("(char)90", 'Z', TestName = "Cast_IntToChar_Z")]
    [TestCase("(int)'9'", 57, TestName = "Cast_DigitCharToInt")]
    [TestCase("(int)(2.5 + 3.7)", 6, TestName = "Cast_ExpressionResult")]
    [TestCase("(int)(-3.9)", -3, TestName = "Cast_NegativeDouble")]
    [TestCase("(double)(10 / 3)", 3.0, TestName = "Cast_IntDivisionResult")]
    [TestCase("(int)(double)42L", 42, TestName = "Cast_ChainedCasts")]
    [TestCase("(int)3.7 + (int)4.2", 7, TestName = "Cast_InArithmetic")]
    [TestCase("(int)3.7m", 3, TestName = "Cast_DecimalToInt")]
    [TestCase("(int?)42", 42, TestName = "Cast_NullableInt")]
    [TestCase("(decimal)42", 42, TestName = "Cast_IntToDecimal")]
    public async Task Eval_Cast(string expr, object expected)
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate(expr);
        var csharpResult = await TestHelpers.EvaluateCSharpAsync(expr);

        Assert.That(result, Is.EqualTo(expected), $"Value mismatch for: {expr}");
        Assert.That(result, Is.EqualTo(csharpResult), $"C# parity mismatch for: {expr}");
        Assert.That(result?.GetType(), Is.EqualTo(csharpResult?.GetType()), $"Type mismatch for: {expr}");
    }

    [Test]
    public async Task Cast_NullToNullableInt()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("(int?)null");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(int?)null");

        Assert.That(result, Is.Null);
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    [Test]
    public async Task Cast_Variable()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 3.7);
        var result = engine.Evaluate("(int)x");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(int)3.7");

        Assert.That(result, Is.EqualTo(3));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }

    [Test]
    public async Task Cast_InConditional()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("x", 3.7);
        var result = engine.Evaluate("(int)x > 3 ? \"yes\" : \"no\"");
        var csharpResult = await TestHelpers.EvaluateCSharpAsync("(int)3.7 > 3 ? \"yes\" : \"no\"");

        Assert.That(result, Is.EqualTo("no"));
        Assert.That(result, Is.EqualTo(csharpResult), "C# parity mismatch");
    }
}

using CsEval.Parsing;

namespace CsEval.Test.Compliance;

[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class EcmaSectionParitySmokeTests(CompilationMode mode)
{
    private CsEvalOptions Options => CsEvalOptions.Default with
    {
        LanguageMode = LanguageMode.Standard
    };

    [Test]
    public void S6_4_1_Tokens_General_MixedTokenStream()
    {
        var lexer = new Lexer("x + 42 == 42 ? true : false");
        var tokens = lexer.Tokenize();

        Assert.That(tokens.Select(t => t.Type), Is.EqualTo(new[]
        {
            TokenType.Identifier,
            TokenType.Plus,
            TokenType.Number,
            TokenType.EqualEqual,
            TokenType.Number,
            TokenType.Question,
            TokenType.True,
            TokenType.Colon,
            TokenType.False,
            TokenType.Eof
        }));
    }

    [Test]
    public async Task S7_5_1_MemberAccess_LengthProperty_Parity()
    {
        await AssertParityAsync("\"hello\".Length");
    }

    [Test]
    public async Task S8_2_3_ObjectType_BoxingAndObjectCheck_Parity()
    {
        await AssertParityAsync("{ object o = 42; return o is object; }");
    }

    [Test]
    public async Task S8_3_9_BoolType_LogicalExpression_Parity()
    {
        await AssertParityAsync("true && !false");
    }

    [Test]
    public async Task S12_12_7_ReferenceEquality_DistinctObjects_AreNotEqual_Parity()
    {
        await AssertParityAsync("{ var a = new object(); var b = new object(); return a == b; }");
    }

    [Test]
    public async Task S12_12_8_StringEquality_ByValue_Parity()
    {
        await AssertParityAsync("\"abc\" == \"ab\" + \"c\"");
    }

    [Test]
    public async Task S12_22_Expression_EvaluatesToValue_Parity()
    {
        await AssertParityAsync("1 + 2 * 3");
    }

    [Test]
    public async Task S12_24_BooleanExpression_IfCondition_Parity()
    {
        await AssertParityAsync("{ if (5 > 3) return 1; return 0; }");
    }

    [Test]
    public async Task S13_7_ExpressionStatement_AssignmentForm_Parity()
    {
        await AssertParityAsync("{ var x = 1; x = x + 2; return x; }");
    }

    private async Task AssertParityAsync(string expression)
    {
        var engine = TestEngineFactory.Create(mode, Options);
        var csEvalResult = engine.Evaluate(expression);
        var roslynResult = await TestHelpers.EvaluateCSharpAsync(expression);

        Assert.That(csEvalResult, Is.EqualTo(roslynResult), $"Value mismatch for: {expression}");
        Assert.That(csEvalResult?.GetType(), Is.EqualTo(roslynResult?.GetType()), $"Type mismatch for: {expression}");
    }
}

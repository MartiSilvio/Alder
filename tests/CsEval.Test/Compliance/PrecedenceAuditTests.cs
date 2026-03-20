using CsEval.Test._Infrastructure;

namespace CsEval.Test.Compliance;

/// <summary>
/// Precedence and associativity audit tests verifying ExpressionParser against ECMA-334 §12.4.2.
/// Each test exercises a specific precedence boundary or associativity rule.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class PrecedenceAuditTests(CompilationMode mode)
{
    private object? Eval(string expr)
        => TestEngineFactory.Create(mode, CsEvalOptions.Default).Evaluate(expr);

    private async Task AssertParityAsync(string expression)
    {
        var engine = TestEngineFactory.Create(mode, CsEvalOptions.Default);
        var csEvalResult = engine.Evaluate(expression);
        var roslynResult = await TestHelpers.EvaluateCSharpAsync(expression);

        Assert.That(csEvalResult, Is.EqualTo(roslynResult), $"Value mismatch for: {expression}");
        Assert.That(csEvalResult?.GetType(), Is.EqualTo(roslynResult?.GetType()), $"Type mismatch for: {expression}");
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 Multiplicative > Additive
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_MultiplicativeOverAdditive()
    {
        await AssertParityAsync("2 + 3 * 4");
    }

    [Test]
    public async Task Precedence_ModulusOverAdditive()
    {
        await AssertParityAsync("10 + 7 % 3");
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 Additive > Shift
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_AdditiveOverShift()
    {
        await AssertParityAsync("1 + 2 << 3");
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 Shift > Relational
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_ShiftOverRelational()
    {
        await AssertParityAsync("1 << 2 < 8");
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 Relational > Equality
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_RelationalOverEquality()
    {
        await AssertParityAsync("true == 3 > 2");
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 Equality > Bitwise AND > Bitwise XOR > Bitwise OR
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_EqualityOverBitwiseAnd()
    {
        await AssertParityAsync("1 == 1 & 1 == 1");
    }

    [Test]
    public async Task Precedence_BitwiseAndOverBitwiseXor()
    {
        await AssertParityAsync("0xFF & 0x0F ^ 0x0F");
    }

    [Test]
    public async Task Precedence_BitwiseXorOverBitwiseOr()
    {
        await AssertParityAsync("0x0F ^ 0xFF | 0x00");
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 Bitwise OR > Conditional AND > Conditional OR
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_ConditionalAndOverConditionalOr()
    {
        await AssertParityAsync("true || false && false");
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 Conditional OR > Null-coalescing > Conditional
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_NullCoalesceOverConditional()
    {
        // ?? is right-associative and above conditional: (null ?? 3) > 0 ? "yes" : "no"
        await AssertParityAsync("""
            {
                string? a = null;
                string? b = null;
                string c = "found";
                return (a ?? b ?? c).Length > 0 ? true : false;
            }
        """);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Associativity: right-to-left for ??
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Associativity_NullCoalesce_RightToLeft()
    {
        await AssertParityAsync("""
            {
                string? a = null;
                string? b = null;
                string c = "found";
                return a ?? b ?? c;
            }
        """);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Associativity: right-to-left for = assignment chains
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Associativity_Assignment_RightToLeft()
    {
        await AssertParityAsync("""
            {
                var a = 0;
                var b = 0;
                a = b = 42;
                return a + b;
            }
        """);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Associativity: right-to-left for ternary (?:)
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Associativity_Ternary_NestedRightToLeft()
    {
        await AssertParityAsync("true ? 1 : true ? 2 : 3");
    }

    [Test]
    public async Task Associativity_Ternary_NestedInCondition()
    {
        await AssertParityAsync("false ? 1 : false ? 2 : 3");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Unary > Multiplicative
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_UnaryNegationOverMultiplicative()
    {
        await AssertParityAsync("-3 * 2");
    }

    [Test]
    public async Task Precedence_BitwiseNotOverShift()
    {
        await AssertParityAsync("~0 << 1");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Cast at unary level
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_CastOverAdditive()
    {
        await AssertParityAsync("(double)3 + 0.14");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Throw expression in null-coalescing (§12.16)
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void ThrowExpression_InNullCoalesce()
    {
        var result = Eval("""{ string? s = "ok"; return s ?? throw new System.Exception(); }""");
        Assert.That(result, Is.EqualTo("ok"));
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.8 Primary: postfix binds tighter than unary
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_PostfixOverUnary()
    {
        // -arr[0] should be -(arr[0]), not (-arr)[0]
        await AssertParityAsync("""
            {
                var arr = new int[] { 5, 10 };
                return -arr[0];
            }
        """);
    }

    [Test]
    public async Task Precedence_MemberAccessOverUnary()
    {
        await AssertParityAsync("""
            {
                var s = "hello";
                return -s.Length;
            }
        """);
    }

    // ═══════════════════════════════════════════════════════════════════
    // §12.4.2 is/as at relational level
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public async Task Precedence_IsAtRelationalLevel()
    {
        await AssertParityAsync("3 is int == true");
    }

    [Test]
    public async Task Precedence_AsAtRelationalLevel()
    {
        await AssertParityAsync("""
            {
                object o = "hello";
                return (o as string) != null;
            }
        """);
    }
}

using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.PatternMatching;

/// <summary>
/// ECMA-334 §12.8.21 -- Switch expressions.
/// Tests switch expression parsing and evaluation, constant/type/relational/property pattern arms,
/// when guards (section 12.8.21.3), discard catch-all (section 11.2.8),
/// and non-exhaustive match behavior (CS8509).
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class SwitchExpressionTests(CompilationMode mode)
{

    [Test]
    public void SwitchExpression_NoMatch_ThrowsAlderException()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", (object)99);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("""x switch { 1 => "one" } """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8509));
    }

    [Test]
    public void SwitchExpression_NoMatch_NullValue_ThrowsAlderException()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", (object?)null);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("""x switch { 1 => "one", "hello" => "two" } """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS8509));
    }

    // Pattern variables in one arm should not leak to other arms
    [Test]
    public void SwitchExpression_PatternVariableNotLeaking()
    {
        var engine = TestEngineFactory.Create(mode);
        engine.SetVariable("x", (object)"hello");
        // The variable 's' from the first arm should not be accessible outside
        var result = engine.Evaluate("x switch { string s => s.Length, _ => -1 }");
        Assert.That(result, Is.EqualTo(5));
        // 's' should not be accessible in the engine context after switch
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("s"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0103));
    }

}

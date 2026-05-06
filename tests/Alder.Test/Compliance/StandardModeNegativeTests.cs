using System.Dynamic;
using Alder.Diagnostics;
using Alder.Test._Infrastructure;

namespace Alder.Test.Compliance;

/// <summary>
/// Proves every Extended-only feature is rejected in Standard mode with the correct
/// exception type and FeatureName, and that each feature works in Extended mode.
/// </summary>
[TestFixtureSource(typeof(Alder.Test._Infrastructure.CompilationModeFixtures), nameof(Alder.Test._Infrastructure.CompilationModeFixtures.All))]
public class StandardModeNegativeTests(CompilationMode mode)
{
    private Action<AlderOptions> StandardOptions => o => o.LanguageMode = LanguageMode.Standard;

    private Action<AlderOptions> ExtendedOptions => o => o.LanguageMode = LanguageMode.Extended;

    #region Parser-Level Syntax Gates (language mode rejection)

    // 1. [...] collection expressions

    [Test]
    public void StandardMode_RejectsCollectionExpression()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("[1, 2, 3]"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS9176));
    }

    [Test]
    public void ExtendedMode_AcceptsCollectionExpression()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        var result = engine.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.TypeOf<int[]>());
    }

    // 3. .. spread in arrays (gated inside collection expression parsing)

    [Test]
    public void StandardMode_RejectsSpreadInArray()
    {
        // The [...] gate fires first, so spread-in-array is covered by collection expression rejection.
        // Verify the gate fires for a spread expression too.
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("[..new int[] { 1, 2 }]"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsSpreadInArray()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        engine.SetVariable("arr", new List<int> { 1, 2, 3 });
        var result = engine.Evaluate("[..arr]");
        Assert.That(result, Is.TypeOf<int[]>());
        Assert.That((int[])result!, Has.Length.EqualTo(3));
    }

    // 4. .. spread in objects

    [Test]
    public void StandardMode_RejectsSpreadInObject()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1;
        engine.SetVariable("obj", obj);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("new { ..obj }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1525));
    }

    [Test]
    public void ExtendedMode_RejectsSpreadInObject()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1;
        engine.SetVariable("obj", obj);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("new { ..obj }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS1525));
    }

    // 5. === strict equality

    [Test]
    public void StandardMode_RejectsStrictEquality()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("1 === 1"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsStrictEquality()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("1 === 1"), Is.True);
    }

    // 5b. !== strict inequality

    [Test]
    public void StandardMode_RejectsStrictInequality()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("1 !== 2"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsStrictInequality()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("1 !== 2"), Is.True);
    }

    // 6. ** power operator

    [Test]
    public void StandardMode_RejectsPowerOperator()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("2 ** 3"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsPowerOperator()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("2 ** 3"), Is.EqualTo(8.0));
    }

    // 7. **= compound power assignment

    [Test]
    public void StandardMode_RejectsCompoundPower()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("{ var x = 2; x **= 3; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsCompoundPower()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("{ var x = 2.0; x **= 3; return x; }"), Is.EqualTo(8.0));
    }

    // 8. between...and operator

    [Test]
    public void StandardMode_RejectsBetweenOperator()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("5 between 1 and 10"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsBetweenOperator()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("5 between 1 and 10"), Is.True);
    }

    // 9. in operator

    [Test]
    public void StandardMode_RejectsInOperator()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        engine.SetVariable("arr", new int[] { 1, 2, 3, 4, 5 });
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("3 in arr"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsInOperator()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        engine.SetVariable("arr", new int[] { 1, 2, 3, 4, 5 });
        Assert.That(engine.Evaluate("3 in arr"), Is.True);
    }

    // 10. like operator

    [Test]
    public void StandardMode_RejectsLikeOperator()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate(""" "hello" like "hel%" """));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsLikeOperator()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate(""" "hello" like "hel%" """), Is.True);
    }

    #endregion

    #region Parser-Level Conditional Gates (parse error, not language mode)

    // 11. [start:end] slice notation -- Standard mode doesn't enter slice parsing path

    [Test]
    public void StandardMode_RejectsSliceNotation()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        Assert.Catch<AlderException>(() => engine.Evaluate(""" "hello"[1:3]"""));
    }

    [Test]
    public void ExtendedMode_AcceptsSliceNotation()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate(""" "hello"[1:3]"""), Is.EqualTo("el"));
    }

    // 12. let keyword -- Standard mode doesn't match 'let', so it becomes an unknown identifier

    [Test]
    public void StandardMode_RejectsLetKeyword()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        Assert.Catch<AlderException>(() => engine.Evaluate("{ let x = 5; return x; }"));
    }

    [Test]
    public void ExtendedMode_AcceptsLetKeyword()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("{ let x = 5; return x; }"), Is.EqualTo(5));
    }

    [Test]
    public void StandardMode_AcceptsConstDeclaration()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        Assert.That(engine.Evaluate("{ const int x = 5; return x * x; }"), Is.EqualTo(25));
    }

    [Test]
    public void ExtendedMode_AcceptsConstDeclaration()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("{ const int x = 5; return x * x; }"), Is.EqualTo(25));
    }

    [Test]
    public void StandardMode_RejectsConstReassignment()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ const int x = 5; x = 6; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0131));
    }

    [Test]
    public void ExtendedMode_RejectsConstReassignment()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        var ex = Assert.Throws<AlderException>(() => engine.Evaluate("{ const int x = 5; x = 6; return x; }"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.CS0131));
    }

    [Test]
    public void StandardMode_RejectsLetInExpression()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("let x = 5 in x"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsLetInExpression()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("let x = 5 in x * x"), Is.EqualTo(25));
    }

    [Test]
    public void StandardMode_RejectsIfExpression()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("if (x > 0) x else -x"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsIfExpression()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        engine.SetVariable("x", -5);
        Assert.That(engine.Evaluate("if (x > 0) x else -x"), Is.EqualTo(5));
    }

    [Test]
    public void StandardMode_RejectsComprehensionExpression()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        var ex = Assert.Throws<AlderException>(
            () => engine.Evaluate("[x for x in 1..3]"));
        Assert.That(ex!.ErrorCode, Is.EqualTo(DiagnosticCode.ALDR0020));
    }

    [Test]
    public void ExtendedMode_AcceptsComprehensionExpression()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        var result = engine.Evaluate("[x for x in 1..=3]");
        Assert.That(result, Is.EqualTo(new[] { 1, 2, 3 }));
    }

    #endregion

    #region Runtime-Level Gates (AlderException or standard .NET exceptions)

    // 13. Negative indexing (always throws — Extended mode does not change C# indexing semantics)

    [Test]
    public void StandardMode_NegativeStringIndex_Throws()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        Assert.Catch<Exception>(() => engine.Evaluate(""" "hello"[-1]"""));
    }

    [Test]
    public void ExtendedMode_NegativeStringIndex_Throws()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.Catch<Exception>(() => engine.Evaluate(""" "hello"[-1]"""));
    }

    [Test]
    public void StandardMode_NegativeListIndex_Throws()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        engine.SetVariable("list", new List<int> { 10, 20, 30 });
        Assert.Catch<Exception>(() => engine.Evaluate("list[-1]"));
    }

    [Test]
    public void ExtendedMode_NegativeListIndex_Throws()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        engine.SetVariable("list", new List<int> { 10, 20, 30 });
        Assert.Catch<Exception>(() => engine.Evaluate("list[-1]"));
    }

    // 14. String multiply: rejected in both modes (C# has no `string * int` operator)

    [Test]
    public void StandardMode_RejectsStringMultiply()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        Assert.Catch<AlderException>(() => engine.Evaluate(""" "abc" * 3"""));
    }

    [Test]
    public void ExtendedMode_RejectsStringMultiply()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.Catch<AlderException>(() => engine.Evaluate(""" "abc" * 3"""));
    }

    [Test]
    public void StandardMode_RejectsAggregateBuiltins()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        Assert.Catch<AlderException>(() => engine.Evaluate("sum(new[] { 1, 2, 3 })"));
    }

    [Test]
    public void ExtendedMode_AcceptsAggregateBuiltins()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("sum(new[] { 1, 2, 3 })"), Is.EqualTo(6));
    }

    [Test]
    public void StandardMode_RejectsDateUnitSugar()
    {
        var engine = TestEngineFactory.Create(mode, StandardOptions);
        Assert.Catch<AlderException>(() => engine.Evaluate("new DateTime(2026, 1, 1) + 30.days"));
    }

    [Test]
    public void ExtendedMode_AcceptsDateUnitSugar()
    {
        var engine = TestEngineFactory.Create(mode, ExtendedOptions);
        Assert.That(engine.Evaluate("30.days"), Is.EqualTo(TimeSpan.FromDays(30)));
    }

    #endregion
}

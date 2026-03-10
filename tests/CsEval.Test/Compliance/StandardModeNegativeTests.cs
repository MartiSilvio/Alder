using System.Dynamic;
using CsEval.Parsing;

namespace CsEval.Test.Compliance;

/// <summary>
/// Proves every Extended-only feature is rejected in Standard mode with the correct
/// exception type and FeatureName, and that each feature works in Extended mode.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
public class StandardModeNegativeTests(CompilationMode mode)
{
    private CsEvalOptions StandardOptions => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        LanguageMode = LanguageMode.Standard
    };

    private CsEvalOptions ExtendedOptions => CsEvalOptions.Default with
    {
        CompilationMode = mode,
        LanguageMode = LanguageMode.Extended
    };

    #region Parser-Level Syntax Gates (CsEvalLanguageModeException)

    // 1. undefined keyword

    [Test]
    public void StandardMode_RejectsUndefined()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("undefined"));
        Assert.That(ex!.FeatureName, Is.EqualTo("undefined"));
    }

    [Test]
    public void ExtendedMode_AcceptsUndefined()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("undefined"), Is.Null);
    }

    // 2. [...] collection expressions

    [Test]
    public void StandardMode_RejectsCollectionExpression()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("[1, 2, 3]"));
        Assert.That(ex!.FeatureName, Is.EqualTo("[...]"));
    }

    [Test]
    public void ExtendedMode_AcceptsCollectionExpression()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        var result = engine.Evaluate("[1, 2, 3]");
        Assert.That(result, Is.TypeOf<int[]>());
    }

    // 3. .. spread in arrays (gated inside collection expression parsing)

    [Test]
    public void StandardMode_RejectsSpreadInArray()
    {
        // The [...] gate fires first, so spread-in-array is covered by collection expression rejection.
        // Verify the gate fires for a spread expression too.
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("[..new int[] { 1, 2 }]"));
        Assert.That(ex!.FeatureName, Is.EqualTo("[...]"));
    }

    [Test]
    public void ExtendedMode_AcceptsSpreadInArray()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        engine.SetVariable("arr", new List<int> { 1, 2, 3 });
        var result = engine.Evaluate("[..arr]");
        Assert.That(result, Is.TypeOf<int[]>());
        Assert.That((int[])result!, Has.Length.EqualTo(3));
    }

    // 4. .. spread in objects

    [Test]
    public void StandardMode_RejectsSpreadInObject()
    {
        var engine = new CsEvalEngine(StandardOptions);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1;
        engine.SetVariable("obj", obj);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("new { ..obj }"));
        Assert.That(ex!.FeatureName, Is.EqualTo(".."));
    }

    [Test]
    public void ExtendedMode_AcceptsSpreadInObject()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        IDictionary<string, object?> obj = new ExpandoObject();
        obj["A"] = 1;
        engine.SetVariable("obj", obj);
        var result = engine.Evaluate("new { ..obj }") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
    }

    // 5. === strict equality

    [Test]
    public void StandardMode_RejectsStrictEquality()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("1 === 1"));
        Assert.That(ex!.FeatureName, Is.EqualTo("==="));
    }

    [Test]
    public void ExtendedMode_AcceptsStrictEquality()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("1 === 1"), Is.True);
    }

    // 5b. !== strict inequality

    [Test]
    public void StandardMode_RejectsStrictInequality()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("1 !== 2"));
        Assert.That(ex!.FeatureName, Is.EqualTo("!=="));
    }

    [Test]
    public void ExtendedMode_AcceptsStrictInequality()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("1 !== 2"), Is.True);
    }

    // 6. ** power operator

    [Test]
    public void StandardMode_RejectsPowerOperator()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("2 ** 3"));
        Assert.That(ex!.FeatureName, Is.EqualTo("**"));
    }

    [Test]
    public void ExtendedMode_AcceptsPowerOperator()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("2 ** 3"), Is.EqualTo(8.0));
    }

    // 7. **= compound power assignment

    [Test]
    public void StandardMode_RejectsCompoundPower()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("{ var x = 2; x **= 3; return x; }"));
        Assert.That(ex!.FeatureName, Is.EqualTo("**="));
    }

    [Test]
    public void ExtendedMode_AcceptsCompoundPower()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("{ var x = 2.0; x **= 3; return x; }"), Is.EqualTo(8.0));
    }

    // 8. between...and operator

    [Test]
    public void StandardMode_RejectsBetweenOperator()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("5 between 1 and 10"));
        Assert.That(ex!.FeatureName, Is.EqualTo("between"));
    }

    [Test]
    public void ExtendedMode_AcceptsBetweenOperator()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("5 between 1 and 10"), Is.True);
    }

    // 9. in operator

    [Test]
    public void StandardMode_RejectsInOperator()
    {
        var engine = new CsEvalEngine(StandardOptions);
        engine.SetVariable("arr", new int[] { 1, 2, 3, 4, 5 });
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("3 in arr"));
        Assert.That(ex!.FeatureName, Is.EqualTo("in"));
    }

    [Test]
    public void ExtendedMode_AcceptsInOperator()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        engine.SetVariable("arr", new int[] { 1, 2, 3, 4, 5 });
        Assert.That(engine.Evaluate("3 in arr"), Is.True);
    }

    // 10. like operator

    [Test]
    public void StandardMode_RejectsLikeOperator()
    {
        var engine = new CsEvalEngine(StandardOptions);
        var ex = Assert.Throws<CsEvalLanguageModeException>(
            () => engine.Evaluate("\"hello\" like \"hel%\""));
        Assert.That(ex!.FeatureName, Is.EqualTo("like"));
    }

    [Test]
    public void ExtendedMode_AcceptsLikeOperator()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("\"hello\" like \"hel%\""), Is.True);
    }

    #endregion

    #region Parser-Level Conditional Gates (parse error, not CsEvalLanguageModeException)

    // 11. [start:end] slice notation -- Standard mode doesn't enter slice parsing path

    [Test]
    public void StandardMode_RejectsSliceNotation()
    {
        var engine = new CsEvalEngine(StandardOptions);
        Assert.Catch<CsEvalException>(() => engine.Evaluate("\"hello\"[1:3]"));
    }

    [Test]
    public void ExtendedMode_AcceptsSliceNotation()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("\"hello\"[1:3]"), Is.EqualTo("el"));
    }

    // 12. let keyword -- Standard mode doesn't match 'let', so it becomes an unknown identifier

    [Test]
    public void StandardMode_RejectsLetKeyword()
    {
        var engine = new CsEvalEngine(StandardOptions);
        Assert.Catch<CsEvalException>(() => engine.Evaluate("{ let x = 5; return x; }"));
    }

    [Test]
    public void ExtendedMode_AcceptsLetKeyword()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("{ let x = 5; return x; }"), Is.EqualTo(5));
    }

    #endregion

    #region Runtime-Level Gates (CsEvalException or standard .NET exceptions)

    // 13. Negative indexing (wraps in Extended, throws in Standard)

    [Test]
    public void StandardMode_NegativeStringIndex_Throws()
    {
        var engine = new CsEvalEngine(StandardOptions);
        Assert.Catch<Exception>(() => engine.Evaluate("\"hello\"[-1]"));
    }

    [Test]
    public void ExtendedMode_NegativeStringIndex_Wraps()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("\"hello\"[-1]"), Is.EqualTo('o'));
    }

    [Test]
    public void StandardMode_NegativeListIndex_Throws()
    {
        var engine = new CsEvalEngine(StandardOptions);
        engine.SetVariable("list", new List<int> { 10, 20, 30 });
        Assert.Catch<Exception>(() => engine.Evaluate("list[-1]"));
    }

    [Test]
    public void ExtendedMode_NegativeListIndex_Wraps()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        engine.SetVariable("list", new List<int> { 10, 20, 30 });
        Assert.That(engine.Evaluate("list[-1]"), Is.EqualTo(30));
    }

    // 14. String multiply (Extended: "abc" * 3 = "abcabcabc"; Standard: throws)

    [Test]
    public void StandardMode_RejectsStringMultiply()
    {
        var engine = new CsEvalEngine(StandardOptions);
        Assert.Catch<CsEvalException>(() => engine.Evaluate("\"abc\" * 3"));
    }

    [Test]
    public void ExtendedMode_AcceptsStringMultiply()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        Assert.That(engine.Evaluate("\"abc\" * 3"), Is.EqualTo("abcabcabc"));
    }

    // 15. Object merge via + (Extended: merges dictionaries; Standard: throws CS0019)

    [Test]
    public void StandardMode_RejectsObjectMerge()
    {
        var engine = new CsEvalEngine(StandardOptions);
        IDictionary<string, object?> left = new ExpandoObject();
        left["A"] = 1;
        IDictionary<string, object?> right = new ExpandoObject();
        right["B"] = 2;
        engine.SetVariable("left", left);
        engine.SetVariable("right", right);
        Assert.Catch<CsEvalException>(() => engine.Evaluate("left + right"));
    }

    [Test]
    public void ExtendedMode_AcceptsObjectMerge()
    {
        var engine = new CsEvalEngine(ExtendedOptions);
        IDictionary<string, object?> left = new ExpandoObject();
        left["A"] = 1;
        IDictionary<string, object?> right = new ExpandoObject();
        right["B"] = 2;
        engine.SetVariable("left", left);
        engine.SetVariable("right", right);
        var result = engine.Evaluate("left + right") as IDictionary<string, object?>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result!["A"], Is.EqualTo(1));
        Assert.That(result["B"], Is.EqualTo(2));
    }

    #endregion
}

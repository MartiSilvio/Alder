namespace CsEval.Test.Runtime;

/// <summary>
/// Tests for tuple deconstruction (ECMA-334 §12.7 - Deconstruction).
/// var (x, y) = tupleExpr creates individual variables from tuple elements.
/// </summary>
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class DeconstructionTests(CompilationMode mode)
{
    #region ECMA-334 §12.7 - Basic deconstruction

    [Test]
    public void Deconstruct_TwoInts_XPlusY()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (1, 2); return x + y; }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Deconstruct_TwoInts_FirstElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (1, 2); return x; }");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Deconstruct_TwoInts_SecondElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (1, 2); return y; }");
        Assert.That(result, Is.EqualTo(2));
    }

    #endregion

    #region ECMA-334 §12.7 - Mixed types

    [Test]
    public void Deconstruct_StringAndInt_Name()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (name, age) = (\"Alice\", 30); return name; }");
        Assert.That(result, Is.EqualTo("Alice"));
    }

    [Test]
    public void Deconstruct_StringAndInt_Age()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (name, age) = (\"Alice\", 30); return age; }");
        Assert.That(result, Is.EqualTo(30));
    }

    [Test]
    public void Deconstruct_BoolAndString()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (flag, msg) = (true, \"ok\"); return flag; }");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion

    #region ECMA-334 §12.7 - Three or more elements

    [Test]
    public void Deconstruct_ThreeElements_Sum()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (a, b, c) = (1, 2, 3); return a + b + c; }");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Deconstruct_FourElements_LastElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (a, b, c, d) = (1, 2, 3, 4); return d; }");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void Deconstruct_FiveElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (a, b, c, d, e) = (10, 20, 30, 40, 50); return a + e; }");
        Assert.That(result, Is.EqualTo(60));
    }

    [Test]
    public void Deconstruct_SixElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (a, b, c, d, e, f) = (1, 2, 3, 4, 5, 6); return f; }");
        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void Deconstruct_SevenElements()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (a, b, c, d, e, f, g) = (1, 2, 3, 4, 5, 6, 7); return g; }");
        Assert.That(result, Is.EqualTo(7));
    }

    #endregion

    #region ECMA-334 §12.7 - With expressions

    [Test]
    public void Deconstruct_WithExpressions_FirstElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (1 + 2, 3 * 4); return x; }");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Deconstruct_WithExpressions_SecondElement()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (1 + 2, 3 * 4); return y; }");
        Assert.That(result, Is.EqualTo(12));
    }

    #endregion

    #region ECMA-334 §12.7 - Chaining with other operations

    [Test]
    public void Deconstruct_Comparison()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (10, 20); return x > y; }");
        Assert.That(result, Is.EqualTo(false));
    }

    [Test]
    public void Deconstruct_Arithmetic()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (min, max) = (1, 100); return max - min; }");
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void Deconstruct_StringConcatenation()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (first, last) = (\"John\", \"Doe\"); return first + \" \" + last; }");
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    #endregion

    #region ECMA-334 §12.7 - Error cases

    [Test]
    public void Deconstruct_NonTuple_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        engine.SetVariable("val", 42);
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y) = val; return x; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot deconstruct non-tuple value"));
    }

    [Test]
    public void Deconstruct_ArityMismatch_TooMany_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y, z) = (1, 2); return x; }"));
        Assert.That(ex!.Message, Does.Contain("Deconstruction requires 3 values but tuple has 2"));
    }

    [Test]
    public void Deconstruct_ArityMismatch_TooFew_Throws()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y) = (1, 2, 3); return x; }"));
        Assert.That(ex!.Message, Does.Contain("Deconstruction requires 2 values but tuple has 3"));
    }

    #endregion

    #region ECMA-334 §12.7 - Type correctness

    [Test]
    public void Deconstruct_PreservesIntType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (42, 99); return x; }");
        Assert.That(result, Is.TypeOf<int>());
    }

    [Test]
    public void Deconstruct_PreservesStringType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (x, y) = (\"hello\", 42); return x; }");
        Assert.That(result, Is.TypeOf<string>());
    }

    [Test]
    public void Deconstruct_PreservesBoolType()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        var result = engine.Evaluate("{ var (a, b, c) = (1, \"hello\", true); return c; }");
        Assert.That(result, Is.TypeOf<bool>());
        Assert.That(result, Is.EqualTo(true));
    }

    [Test]
    public void Deconstruct_MixedTypes_AllCorrect()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with { CompilationMode = mode });
        // Verify each element has the correct type after deconstruction
        var result1 = engine.Evaluate("{ var (a, b, c) = (1, \"hello\", true); return a; }");
        Assert.That(result1, Is.TypeOf<int>());
        Assert.That(result1, Is.EqualTo(1));

        var result2 = engine.Evaluate("{ var (a, b, c) = (1, \"hello\", true); return b; }");
        Assert.That(result2, Is.TypeOf<string>());
        Assert.That(result2, Is.EqualTo("hello"));

        var result3 = engine.Evaluate("{ var (a, b, c) = (1, \"hello\", true); return c; }");
        Assert.That(result3, Is.TypeOf<bool>());
        Assert.That(result3, Is.EqualTo(true));
    }

    #endregion
}

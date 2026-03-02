namespace CsEval.Test.Extensions;

// C# deconstruction (C#)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class DeconstructionEnhancedTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode });

    #region Tuple deconstruction (existing, regression)

    [Test]
    public void Deconstruct_Tuple_FirstElement()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var (a, b) = (1, 2); a");
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void Deconstruct_Tuple_SecondElement()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var (a, b) = (1, 2); b");
        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void Deconstruct_Tuple_ThreeElements_Sum()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var (x, y, z) = (10, 20, 30); x + y + z");
        Assert.That(result, Is.EqualTo(60));
    }

    #endregion

    #region Deconstruct() method

    [Test]
    public void Deconstruct_CustomType_ReturnsFirstField()
    {
        var engine = CreateEngine();
        var point = new TestPoint(3, 4);
        engine.SetVariable("point", point);
        var result = engine.Evaluate("var (a, b) = point; a");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Deconstruct_CustomType_ReturnsSecondField()
    {
        var engine = CreateEngine();
        var point = new TestPoint(3, 4);
        engine.SetVariable("point", point);
        var result = engine.Evaluate("var (a, b) = point; b");
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void Deconstruct_CustomType_UsedInExpression()
    {
        var engine = CreateEngine();
        var point = new TestPoint(3, 4);
        engine.SetVariable("point", point);
        var result = engine.Evaluate("var (x, y) = point; x * x + y * y");
        Assert.That(result, Is.EqualTo(25));
    }

    #endregion

    #region KeyValuePair (built-in Deconstruct)

    [Test]
    public void Deconstruct_KeyValuePair_Key()
    {
        var engine = CreateEngine();
        engine.SetVariable("kvp", new KeyValuePair<string, int>("hello", 42));
        var result = engine.Evaluate("var (key, val) = kvp; key");
        Assert.That(result, Is.EqualTo("hello"));
    }

    [Test]
    public void Deconstruct_KeyValuePair_Value()
    {
        var engine = CreateEngine();
        engine.SetVariable("kvp", new KeyValuePair<string, int>("hello", 42));
        var result = engine.Evaluate("var (key, val) = kvp; val");
        Assert.That(result, Is.EqualTo(42));
    }

    #endregion

    #region Standard mode (deconstruction is standard C#)

    [Test]
    public void Deconstruct_StandardMode_TupleWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Standard
        });
        var result = engine.Evaluate("var (a, b) = (1, 2); a + b");
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void Deconstruct_StandardMode_DeconstructMethodWorks()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode,
            LanguageMode = LanguageMode.Standard
        });
        engine.SetVariable("point", new TestPoint(5, 6));
        var result = engine.Evaluate("var (x, y) = point; x + y");
        Assert.That(result, Is.EqualTo(11));
    }

    #endregion

    #region Error cases

    [Test]
    public void Deconstruct_NoDeconstructMethod_Throws()
    {
        var engine = CreateEngine();
        engine.SetVariable("val", 42);
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y) = val; return x; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot deconstruct"));
    }

    [Test]
    public void Deconstruct_Null_Throws()
    {
        var engine = CreateEngine();
        engine.SetVariable("val", (object?)null);
        var ex = Assert.Throws<CsEvalException>(() =>
            engine.Evaluate("{ var (x, y) = val; return x; }"));
        Assert.That(ex!.Message, Does.Contain("Cannot deconstruct"));
    }

    #endregion

    #region Test helper types

    /// <summary>
    /// Simple type with a Deconstruct() method for testing.
    /// </summary>
    public class TestPoint(int x, int y)
    {
        public int X { get; } = x;
        public int Y { get; } = y;

        public void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }
    }

    #endregion
}

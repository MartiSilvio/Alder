namespace CsEval.Test.Extensions;

// Slice step (Python)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class SliceStepTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    private CsEvalEngine CreateEngineWithList()
    {
        var engine = CreateEngine();
        engine.SetVariable("list", new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        return engine;
    }

    #region Every-other element

    [Test]
    public void Eval_SliceStep2_ReturnsEveryOtherElement()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[::2]");
        Assert.That(result, Is.EqualTo(new List<int> { 0, 2, 4, 6, 8 }));
    }

    [Test]
    public void Eval_SliceStep2_StartAt1_ReturnsOddIndices()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[1::2]");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 3, 5, 7, 9 }));
    }

    #endregion

    #region Reverse

    [Test]
    public void Eval_SliceStepNeg1_ReturnsReversedList()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[::-1]");
        Assert.That(result, Is.EqualTo(new List<int> { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }));
    }

    [Test]
    public void Eval_SliceStepNeg2_ReturnsEveryOtherReversed()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[::-2]");
        Assert.That(result, Is.EqualTo(new List<int> { 9, 7, 5, 3, 1 }));
    }

    #endregion

    #region Bounded with step

    [Test]
    public void Eval_SliceBoundedStep2_ReturnsSteppedSubsequence()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[1:7:2]");
        Assert.That(result, Is.EqualTo(new List<int> { 1, 3, 5 }));
    }

    [Test]
    public void Eval_SliceBoundedStep3_ReturnsSteppedSubsequence()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[0:10:3]");
        Assert.That(result, Is.EqualTo(new List<int> { 0, 3, 6, 9 }));
    }

    #endregion

    #region Negative step with bounds

    [Test]
    public void Eval_SliceNegStep_BoundedReverse()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[8:2:-1]");
        Assert.That(result, Is.EqualTo(new List<int> { 8, 7, 6, 5, 4, 3 }));
    }

    [Test]
    public void Eval_SliceNegStep2_BoundedReverse()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[9:0:-2]");
        Assert.That(result, Is.EqualTo(new List<int> { 9, 7, 5, 3, 1 }));
    }

    #endregion

    #region Edge cases

    [Test]
    public void Eval_SliceStepZero_ThrowsError()
    {
        var engine = CreateEngineWithList();
        Assert.That(() => engine.Evaluate("list[::0]"),
            Throws.InstanceOf<CsEvalException>()
                .With.Message.Contains("step cannot be zero"));
    }

    [Test]
    public void Eval_SliceStep1_ReturnsSameAsNoStep()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[::1]");
        Assert.That(result, Is.EqualTo(new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
    }

    [Test]
    public void Eval_SliceStartGreaterThanEndPositiveStep_ReturnsEmpty()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[5:2:1]");
        Assert.That(result, Is.EqualTo(new List<int>()));
    }

    #endregion

    #region String slicing

    [Test]
    public void Eval_StringSliceStep2_ReturnsEveryOtherChar()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("\"abcdefghij\"[::2]");
        Assert.That(result, Is.EqualTo("acegi"));
    }

    [Test]
    public void Eval_StringSliceReverse_ReturnsReversedString()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("\"abcdefghij\"[::-1]");
        Assert.That(result, Is.EqualTo("jihgfedcba"));
    }

    #endregion

    #region Array slicing

    [Test]
    public void Eval_ArraySliceStep2_ReturnsEveryOtherElement()
    {
        var engine = CreateEngine();
        engine.SetVariable("arr", new[] { 1, 2, 3, 4, 5 });
        var result = engine.Evaluate("arr[::2]");
        Assert.That(result, Is.EqualTo(new[] { 1, 3, 5 }));
    }

    #endregion

    #region Existing two-arg regression

    [Test]
    public void Eval_TwoArgSlice_MiddleRange_Unchanged()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[2:5]");
        Assert.That(result, Is.EqualTo(new List<int> { 2, 3, 4 }));
    }

    [Test]
    public void Eval_TwoArgSlice_FromStart_Unchanged()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[:3]");
        Assert.That(result, Is.EqualTo(new List<int> { 0, 1, 2 }));
    }

    [Test]
    public void Eval_TwoArgSlice_ToEnd_Unchanged()
    {
        var engine = CreateEngineWithList();
        var result = engine.Evaluate("list[7:]");
        Assert.That(result, Is.EqualTo(new List<int> { 7, 8, 9 }));
    }

    #endregion

    #region Standard mode rejection

    [Test]
    public void Eval_StandardMode_ThreeArgSlice_Rejected()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode, LanguageMode = LanguageMode.Standard
        });
        // Standard mode rejects all slicing (two-arg or three-arg)
        Assert.Catch<CsEvalException>(() => engine.Evaluate("new[] { 1, 2, 3 }[::2]"));
    }

    #endregion
}

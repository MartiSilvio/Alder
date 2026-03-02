namespace CsEval.Test.Extensions;

// Pipeline operator (F#/Elixir)
[TestFixture(CompilationMode.Interpreted)]
[TestFixture(CompilationMode.Compiled)]
[TestFixture(CompilationMode.StrictCompiled)]
public class PipelineOperatorTests(CompilationMode mode)
{
    private CsEvalEngine CreateEngine() =>
        new(CsEvalOptions.Default with { CompilationMode = mode, LanguageMode = LanguageMode.Extended });

    #region Basic pipeline

    [Test]
    public void Eval_PipelineWithLambda_PassesLeftAsArgument()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("5 |> (x => x * 2)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_PipelineStringToUpper_ReturnsUpperCase()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("\"hello\" |> (s => s.ToUpper())");
        Assert.That(result, Is.EqualTo("HELLO"));
    }

    #endregion

    #region Pipeline with named functions

    [Test]
    public void Eval_PipelineWithRegisteredFunction_InvokesFunction()
    {
        var engine = CreateEngine();
        engine.RegisterFunction("dbl", args => Convert.ToInt32(args[0]) * 2);
        var result = engine.Evaluate("5 |> dbl");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_PipelineWithVariableLambda_InvokesLambda()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("var f = (x => x + 1); 5 |> f");
        Assert.That(result, Is.EqualTo(6));
    }

    #endregion

    #region Chained pipeline

    [Test]
    public void Eval_ChainedPipeline_TwoStages_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("5 |> (x => x * 2) |> (x => x + 1)");
        Assert.That(result, Is.EqualTo(11));
    }

    [Test]
    public void Eval_ChainedPipeline_StringOperations_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("\"hello\" |> (s => s.ToUpper()) |> (s => s + \"!\")");
        Assert.That(result, Is.EqualTo("HELLO!"));
    }

    [Test]
    public void Eval_ChainedPipeline_ThreeStages_ReturnsCorrectResult()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("1 |> (x => x + 1) |> (x => x * 3) |> (x => x - 2)");
        Assert.That(result, Is.EqualTo(4));
    }

    #endregion

    #region Pipeline with method-like lambdas

    [Test]
    public void Eval_PipelineTrim_ReturnsTrimmmedString()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("\"  hello  \" |> (s => s.Trim())");
        Assert.That(result, Is.EqualTo("hello"));
    }

    #endregion

    #region Error cases

    [Test]
    public void Eval_PipelineNonCallableInt_ThrowsCsEvalException()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("5 |> 10"),
            Throws.InstanceOf<CsEvalException>()
                .With.Message.Contains("|>"));
    }

    [Test]
    public void Eval_PipelineNonCallableString_ThrowsCsEvalException()
    {
        var engine = CreateEngine();
        Assert.That(() => engine.Evaluate("5 |> \"hello\""),
            Throws.InstanceOf<CsEvalException>()
                .With.Message.Contains("|>"));
    }

    #endregion

    #region Precedence

    [Test]
    public void Eval_PipelinePrecedence_ArithmeticBindsTighter()
    {
        // 2 + 3 |> (x => x * 2) should be (2 + 3) |> (x => x * 2) = 10
        var engine = CreateEngine();
        var result = engine.Evaluate("2 + 3 |> (x => x * 2)");
        Assert.That(result, Is.EqualTo(10));
    }

    [Test]
    public void Eval_PipelinePrecedence_TernaryBindsTighter()
    {
        // false ? 5 : 10 |> (x => x * 2)
        // Ternary's else branch is parsed via ParseExpression, which includes pipeline.
        // So: false ? 5 : (10 |> (x => x * 2)) = false ? 5 : 20 = 20
        var engine = CreateEngine();
        var result = engine.Evaluate("false ? 5 : 10 |> (x => x * 2)");
        Assert.That(result, Is.EqualTo(20));
    }

    #endregion

    #region Standard mode rejection

    [Test]
    public void Eval_StandardMode_PipelineThrows()
    {
        var engine = new CsEvalEngine(CsEvalOptions.Default with
        {
            CompilationMode = mode, LanguageMode = LanguageMode.Standard
        });
        Assert.That(() => engine.Evaluate("5 |> (x => x * 2)"),
            Throws.InstanceOf<CsEvalLanguageModeException>());
    }

    #endregion

    #region Pipeline with null

    [Test]
    public void Eval_PipelineNullToRightSide_ThrowsCsEvalException()
    {
        var engine = CreateEngine();
        // Pipeline with null right side should throw about operator applicability
        Assert.That(() => engine.Evaluate("5 |> null"),
            Throws.InstanceOf<CsEvalException>()
                .With.Message.Contains("|>"));
    }

    [Test]
    public void Eval_PipelineNullLeftSide_PassesNullToFunction()
    {
        var engine = CreateEngine();
        var result = engine.Evaluate("null |> (x => x == null)");
        Assert.That(result, Is.EqualTo(true));
    }

    #endregion
}
